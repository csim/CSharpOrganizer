import * as vscode from "vscode";
import { exec } from "child_process";
import * as path from "path";
import * as fs from "fs";
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

  const command = `"${executablePath}" organize "${path}"`;

  return new Promise((resolve, reject) => {
    exec(command, async (error, stdout, stderr) => {
      if (stdout) {
        log(`stdout: ${stdout}`);
      }

      if (error) {
        log(`error: ${error}`);
        reject(error);
        return;
      }

      if (stderr) {
        log(`stderr: ${stderr}`);
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
