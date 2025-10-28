import * as vscode from "vscode";
import { exec } from "child_process";
import * as path from "path";
import * as fs from "fs";
import { findCSharplyExecutable } from "./utils";

export async function organizeFileCommand(): Promise<void> {
  const activeEditor = vscode.window.activeTextEditor;

  if (!activeEditor) {
    vscode.window.showErrorMessage("No active file to organize");
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
    const result = await organize(filePath, true);
  } catch (error) {
    vscode.window.showErrorMessage(`CSharply error: ${error}`);
  }
}

export async function organizeWorkspaceFoldersCommand(): Promise<void> {
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
  // Try to find the executable
  const executable = await findCSharplyExecutable();

  if (!executable) {
    throw new Error(
      "CSharply executable not found. Please ensure CSharply is installed and in your PATH, or install it in a common location."
    );
  }

  const command = `"${executable}" organize "${path}"`;

  return new Promise((resolve, reject) => {
    exec(command, async (error, stdout, stderr) => {
      if (error) {
        vscode.window.showErrorMessage(`CSharply error: ${error.message}`);
        reject(error);
        return;
      }

      if (stderr) {
        vscode.window.showWarningMessage(`CSharply error: ${stderr}`);
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
