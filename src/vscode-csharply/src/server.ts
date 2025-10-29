import * as vscode from "vscode";
import { exec, ChildProcess, spawn } from "child_process";
import * as net from "net";
import {
  findCliExecutable,
  findOpenPort,
  wait,
  log,
  ensureCliInstalled,
} from "./utils";

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
    const fsPath = activeEditor.document.uri.fsPath;

    if (organizedCode && organizedCode !== fileContents) {
      await activeEditor.edit((editBuilder) => {
        editBuilder.replace(
          new vscode.Range(0, 0, activeEditor.document.lineCount, 0),
          organizedCode
        );
      });

      log(`organized: ${fsPath}`);
    } else {
      log(`no change: ${fsPath}`);
    }

    await activeEditor.document.save();
  } catch (error) {
    log(`error organizing file: ${error}`);
    vscode.window.showErrorMessage(`CSharply: ${error}`);
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
    throw new Error("server not available.");
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

  await ensureCliInstalled();

  isStarting = true;

  try {
    const serverPort = await findOpenPort(8149, 100);

    serverUrl = `http://127.0.0.1:${serverPort}`;

    const executablePath = await findCliExecutable();
    if (!executablePath) {
      throw new Error("CSharply: cli not found, check output for details.");
    }

    log(`starting server: ${executablePath} server --port ${serverPort}`);

    serverProcess = spawn(
      executablePath,
      ["server", "--port", serverPort.toString()],
      {
        detached: false,
      }
    );

    // Wait for spawn event
    await new Promise<void>((resolve, reject) => {
      serverProcess!.on("spawn", () => {
        log(`server started: ${serverUrl}`);
        resolve();
      });

      serverProcess!.on("error", (error) => {
        log(`failed to start server: ${error.message}`);
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
      throw new Error("server started but is not healthy");
    } else {
      log("server is healthy and ready");
    }

    // Set up exit handler for cleanup
    serverProcess.on("exit", (code, signal) => {
      log(`server exited with code ${code}, signal ${signal}`);
      serverProcess = undefined;
      isStarting = false;
    });
  } catch (error) {
    isStarting = false;
    throw error;
  }
}

export async function restart(): Promise<void> {
  stop();

  await wait(1000);

  await start();
}

export function stop(): void {
  if (!serverProcess) {
    return;
  }

  const pid = serverProcess.pid;

  try {
    log(`stopping server process ${pid}...`);
    serverProcess.kill("SIGKILL");
    log(`server process ${pid} terminated`);
  } catch (error) {
    log(`error stopping server: ${error}`);
  } finally {
    // Always cleanup references
    serverProcess = undefined;
    isStarting = false;
    log("server cleanup completed");
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
