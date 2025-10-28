import { exec } from "child_process";
import * as path from "path";
import * as fs from "fs";

let executablePath: string | null = null;

export async function findCSharplyExecutable(): Promise<string | null> {
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
      // Check if file exists for full paths
      if (path.isAbsolute(execPath)) {
        if (fs.existsSync(execPath)) {
          // Verify it works by testing --version
          const isWorking = await testExecutable(execPath);
          if (isWorking) {
            executablePath = execPath;
            return execPath;
          }
        }
      } else {
        // For relative paths (like "csharply"), test directly
        const isWorking = await testExecutable(execPath);
        if (isWorking) {
          executablePath = execPath;
          return execPath;
        }
      }
    } catch (error) {
      // Continue searching
    }
  }

  return null;
}

async function testExecutable(execPath: string): Promise<boolean> {
  return new Promise((resolve) => {
    exec(`"${execPath}" --version`, { timeout: 2000 }, (error) => {
      resolve(!error);
    });
  });
}
