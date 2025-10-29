import * as vscode from "vscode";
import { exec, ChildProcess, spawn } from "child_process";
import * as net from "net";
import { findCSharplyExecutable, wait, log } from "./utils";

let serverProcess: ChildProcess | undefined;
let isStarting = false;
let serverUrl: string = "";

export async function organizeFileCommand() {
  const activeEditor = vscode.window.activeTextEditor;

  if (!activeEditor) {
    return;
  }

  const path = activeEditor.document.uri.fsPath;
  if (!path.endsWith(".cs")) {
    return;
  }

  try {
    const fileContents = activeEditor.document.getText();
    const organizedCode = await organizeCode(fileContents);

    // Update file content
    if (organizedCode && organizedCode !== fileContents) {
      await activeEditor.edit((editBuilder) => {
        editBuilder.replace(
          new vscode.Range(0, 0, activeEditor.document.lineCount, 0),
          organizedCode
        );
      });

      log("File organized successfully.");
    } else {
      log("No changes to file.");
    }
  } catch (error) {
    log(`Error organizing file: ${error}`);
    vscode.window.showErrorMessage(`CSharply error: ${error}`);
  }
}

async function serverHealthy(): Promise<boolean> {
  try {
    const response = await fetch(`${serverUrl}/health`, {
      method: "GET",
      signal: AbortSignal.timeout(1000),
    });

    return response.status === 200;
  } catch (error) {
    return false;
  }
}

async function organizeCode(code: string): Promise<string> {
  // Ensure server is running
  if (!isRunning()) {
    await start();
  }

  // Wait for server to be healthy
  const maxRetries = 20;
  let retries = 0;
  while (retries < maxRetries && !(await serverHealthy())) {
    await wait(500);
    retries++;
  }

  if (retries >= maxRetries) {
    throw new Error("httpserver not available.");
  }

  const response = await fetch(`${serverUrl}/organize`, {
    method: "POST",
    headers: {
      "Content-Type": "text/plain",
    },
    body: code,
  });

  if (!response.ok) {
    throw new Error(`CSharply Error: ${response.status} ${response.text()}`);
  }

  return await response.text();
}

async function start(): Promise<void> {
  if (serverProcess || isStarting) {
    return;
  }

  isStarting = true;

  try {
    // Find an available port
    const serverPort = await findPort();
    serverUrl = `http://localhost:${serverPort}`;
    log(`Using port ${serverPort} for HTTP server`);

    const executablePath = await findCSharplyExecutable();
    if (!executablePath) {
      throw new Error("CSharply executable not found");
    }

    log(`Starting HTTP server from ${executablePath}...`);

    // const executablePath =
    //   "C:\\src\\CSharply\\artifacts\\bin\\CSharply\\debug\\CSharply.exe";

    serverProcess = spawn(
      executablePath,
      ["serve", "--port", serverPort.toString()],
      {
        detached: false,
      }
    );

    // Wait for spawn event
    await new Promise<void>((resolve, reject) => {
      serverProcess!.on("spawn", () => {
        log("HTTP server started");
        resolve();
      });

      serverProcess!.on("error", (error) => {
        log(`Failed to start server: ${error.message}`);
        serverProcess = undefined;
        isStarting = false;
        reject(error);
      });
    });

    // Wait for server to become healthy
    const maxWait = 15000; // 15 seconds
    const checkInterval = 200; // 500ms
    let waited = 0;

    while (waited < maxWait && !(await serverHealthy())) {
      await wait(checkInterval);
      waited += checkInterval;
    }

    isStarting = false;

    if (waited >= maxWait) {
      throw new Error("HTTP server started but is not healthy");
    } else {
      log("HTTP server is healthy and ready");
    }

    // Set up exit handler for cleanup
    serverProcess.on("exit", (code, signal) => {
      log(`HTTP server exited with code ${code}, signal ${signal}`);
      serverProcess = undefined;
      isStarting = false;
    });
  } catch (error) {
    isStarting = false;
    throw error;
  }
}

export async function restart(): Promise<void> {
  stop(); // Now synchronous

  await wait(1000);

  await start();
}

export function stop(): void {
  if (!serverProcess) {
    return;
  }

  const pid = serverProcess.pid;

  try {
    log(`Stopping http server process ${pid}...`);
    serverProcess.kill("SIGKILL");
    log(`http server process ${pid} terminated`);
  } catch (error) {
    log(`Error stopping http server: ${error}`);
  } finally {
    // Always cleanup references
    serverProcess = undefined;
    isStarting = false;
    log("http server cleanup completed");
  }
}

function isRunning(): boolean {
  return (
    serverProcess !== undefined &&
    !serverProcess.killed &&
    serverProcess.exitCode === null &&
    serverProcess.signalCode === null
  );
}

export async function testConnection(): Promise<boolean> {
  try {
    // First check if server process is running
    if (!isRunning()) {
      return false;
    }

    // Then check if server is healthy
    if (!(await serverHealthy())) {
      return false;
    }

    // Finally test actual communication
    await organizeCode("// test");
    return true;
  } catch (error) {
    log(`HTTP connection test failed: ${error}`);
    return false;
  }
}

function getServerStatus(): {
  isRunning: boolean;
  pid?: number;
  isStarting: boolean;
} {
  return {
    isRunning: isRunning(),
    pid: serverProcess?.pid,
    isStarting: isStarting,
  };
}

async function findPort(): Promise<number> {
  const isPortOpen = (port: number): Promise<boolean> => {
    return new Promise((resolve) => {
      const server = net.createServer();

      server.listen(port, () => {
        server.close(() => {
          resolve(true); // Port is available
        });
      });

      server.on("error", () => {
        resolve(false); // Port is in use
      });
    });
  };

  // Try ports starting from 8149
  const startPort = 8149;
  const maxAttempts = 100; // Try 100 ports max

  for (let i = 0; i < maxAttempts; i++) {
    const port = startPort + i;
    if (await isPortOpen(port)) {
      return port;
    }
  }

  throw new Error(
    `No available ports found in range ${startPort}-${
      startPort + maxAttempts - 1
    }`
  );
}
