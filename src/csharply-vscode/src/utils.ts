import { exec } from "child_process";
import * as path from "path";
import * as fs from "fs";
import * as vscode from "vscode";
import * as net from "net";

let executablePath: string | null = null;
let outputChannel: vscode.OutputChannel;

export function showOutput(): void {
  getOutputChannel().show();
}

export async function findCliExecutable(): Promise<string | null> {
  if (executablePath) {
    return executablePath;
  }

  const commonPaths = [
    path.join(
      process.env.USERPROFILE || "",
      ".dotnet",
      "tools",
      "csharply.exe"
    ),
    "csharply.exe",
  ];

  for (const execPath of commonPaths) {
    try {
      if (path.isAbsolute(execPath)) {
        if (fs.existsSync(execPath)) {
          const isWorking = await testCli(execPath);
          if (isWorking) {
            executablePath = execPath;
            return execPath;
          }
        }
      } else {
        const isWorking = await testCli(execPath);
        if (isWorking) {
          executablePath = execPath;
          return execPath;
        }
      }
    } catch (error) {
      // continue
    }
  }

  return null;
}

async function testCli(execPath: string): Promise<boolean> {
  try {
    await new Promise<void>((resolve, reject) => {
      exec(`"${execPath}" --version`, { timeout: 2000 }, (error) => {
        if (error) {
          reject(error);
        } else {
          resolve();
        }
      });
    });
    return true;
  } catch (error) {
    return false;
  }
}

export async function ensureCliInstalled(): Promise<string> {
  const existingPath = await findCliExecutable();
  if (existingPath) {
    return existingPath;
  }

  log("cli not found, attempting to install...");

  // Try to install using dotnet tool
  const installResult = await installCli();
  if (!installResult.Success) {
    throw new Error(`CSharply: Failed to install, check output for details.`);
  }

  log("cli installed successfully, verifying...");

  // Clear cached path and try to find it again
  const newPath = await findCliExecutable();
  if (!newPath) {
    throw new Error(
      "CSharply: installation complete but cli not found, check output for details."
    );
  }

  log(`verified cli installation at: ${newPath}`);

  return newPath;
}

async function installCli(): Promise<{
  Success: boolean;
  Error?: string;
}> {
  try {
    log("running: dotnet tool install csharply --global");

    const { stdout, stderr } = await new Promise<{
      stdout: string;
      stderr: string;
    }>((resolve, reject) => {
      exec(
        "dotnet tool install csharply --global",
        { timeout: 30000 },
        (error, stdout, stderr) => {
          if (error) {
            reject(error);
            return;
          }
          resolve({ stdout, stderr });
        }
      );
    });

    if (stdout) {
      log(`installation stdout: ${stdout}`);
    }
    if (stderr) {
      log(`installation stderr: ${stderr}`);
    }

    log("dotnet tool install completed");

    return { Success: true };
  } catch (error: any) {
    log(`installation error: ${error.message}`);
    return { Success: false, Error: error.message };
  }
}

export async function findOpenPort(
  startPort: number,
  maxAttempts: number = 100
): Promise<number> {
  const isPortAvailable = (port: number): Promise<boolean> => {
    return new Promise((resolve) => {
      const server = net.createServer();

      server.listen(port, "127.0.0.1", () => {
        server.close(() => {
          resolve(true); // Port is available - we could bind to it
        });
      });

      server.on("error", (err: any) => {
        resolve(false); // Port is in use - couldn't bind to it
      });
    });
  };

  for (let i = 0; i < maxAttempts; i++) {
    const port = startPort + i;
    if (await isPortAvailable(port)) {
      return port;
    }
  }

  throw new Error(
    `no available ports found in range ${startPort}-${
      startPort + maxAttempts - 1
    }`
  );
}

export function getOutputChannel(): vscode.OutputChannel {
  if (!outputChannel) {
    outputChannel = vscode.window.createOutputChannel("CSharply");
  }
  return outputChannel;
}

export function log(message: string): void {
  const channel = getOutputChannel();
  const timestamp = new Date().toLocaleTimeString();
  channel.appendLine(`[${timestamp}] ${message}`);
}

export function wait(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}
