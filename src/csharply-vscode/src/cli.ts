import * as vscode from "vscode";
import { spawn } from "child_process";
import { ensureCliInstalled, findCliExecutable, log } from "./utils";

export async function organizeFileCommand(): Promise<void> {
  const activeEditor = vscode.window.activeTextEditor;

  if (!activeEditor) {
    return;
  }

  const filePath = activeEditor.document.uri.fsPath;
  if (!filePath.endsWith(".cs")) {
    return;
  }

  // Save the current file before processing (only if modified)
  if (activeEditor.document.isDirty) {
    try {
      await activeEditor.document.save();
    } catch (saveError) {
      vscode.window.showErrorMessage(`Failed to save file: ${saveError}`);
      return;
    }
  }

  try {
    await organize(filePath, true);
  } catch (error) {
    vscode.window.showErrorMessage(`CSharply error: ${error}`);
  }
}

export async function organizeWorkspaceFoldersCommand(): Promise<void> {
  await ensureCliInstalled();

  const folders = vscode.workspace.workspaceFolders;

  if (!folders || folders.length === 0) {
    vscode.window.showErrorMessage("CSharply: No workspace(s) available.");
    return;
  }

  for (const folder of folders) {
    await organize(folder.uri.fsPath, true);
  }
}

export async function organize(
  path: string,
  displayInfo: boolean
): Promise<void> {
  await ensureCliInstalled();

  const executablePath = await findCliExecutable();
  if (!executablePath) {
    throw new Error("CSharply: cli not found, check output for details.");
  }

  return new Promise((resolve, reject) => {
    const child = spawn(executablePath, ["organize", path]);

    let stdout = "";
    let stderr = "";

    child.stdout.on("data", (data) => {
      const output = data.toString();
      stdout += output;
      log(`stdout: ${output}`);
    });

    child.stderr.on("data", (data) => {
      const output = data.toString();
      stderr += output;
      log(`stderr: ${output}`);
    });

    child.on("error", (error) => {
      log(`error: ${error}`);
      reject(error);
    });

    child.on("close", (code) => {
      if (code !== 0) {
        reject(new Error(`Process exited with code ${code}`));
        return;
      }

      if (displayInfo) {
        vscode.window.showInformationMessage(
          stdout ? `CSharply: ${stdout.trim()}` : "CSharply: Done."
        );
      }

      resolve();
    });
  });
}
