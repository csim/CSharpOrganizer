import * as vscode from "vscode";
import { exec, ChildProcess, spawn } from "child_process";
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

async function isServerHealthy(): Promise<boolean> {
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

  await wait(100);

  const maxRetries = 20;
  let retries = 0;
  while (retries < maxRetries && !(await isServerHealthy())) {
    await wait(100);
    retries++;
  }

  if (retries >= maxRetries) {
    throw new Error("CSharply: server not available.");
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

  await ensureCliInstalled();

  try {
    const serverPort = await findOpenPort(8149, 100);

    serverUrl = `http://127.0.0.1:${serverPort}`;

    const cliPath = await findCliExecutable();
    if (!cliPath) {
      throw new Error("CSharply: cli not found, check output for details.");
    }

    log(`starting server: ${cliPath} server --port ${serverPort}`);

    serverProcess = spawn(
      cliPath,
      ["server", "--port", serverPort.toString()],
      {
        detached: false,
      }
    );

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

    const maxWait = 5000; // 5 seconds
    const checkInterval = 200; // 200ms
    let waited = 0;

    await wait(100);

    while (waited < maxWait && !(await isServerHealthy())) {
      await wait(checkInterval);
      waited += checkInterval;
    }

    isStarting = false;

    if (waited >= maxWait) {
      throw new Error("server started but is not healthy");
    } else {
      log("server is healthy and ready");
    }

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
    log(`stopping server process ${pid} ...`);
    serverProcess.kill("SIGKILL");
    log(`server process ${pid} terminated`);
  } catch (error) {
    log(`error stopping server: ${error}`);
  } finally {
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
