import * as vscode from "vscode";
import { exec } from "child_process";

let organizeFileCommand: vscode.Disposable | undefined;
let organizeFolderCommand: vscode.Disposable | undefined;

export function activate(context: vscode.ExtensionContext) {
  //console.log('Congratulations, your extension "csharply" is now active!');

  organizeFileCommand = vscode.commands.registerCommand(
    "csharply.organize.file",
    async () => {
      const activeEditor = vscode.window.activeTextEditor;

      if (!activeEditor) {
        vscode.window.showErrorMessage("No active file to organize");
        return;
      }

      const path = activeEditor.document.uri.fsPath;
      if (!path.endsWith(".cs")) {
        // vscode.window.showWarningMessage(
        //   "CSharply can only organize C# files (.cs)"
        // );
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

      await organize(path, false);
    }
  );

  organizeFolderCommand = vscode.commands.registerCommand(
    "csharply.organize.folder",
    async () => {
      const folders = vscode.workspace.workspaceFolders;

      if (!folders || folders.length === 0) {
        vscode.window.showErrorMessage("CSharply: No workspace(s) available.");
        return;
      }

      for (const folder of folders) {
        await organize(folder.uri.fsPath, true);
      }
    }
  );

  context.subscriptions.push(organizeFileCommand);
  context.subscriptions.push(organizeFolderCommand);
}

async function organize(path: string, displayInfo: boolean): Promise<void> {
  const command = `csharply.exe organize "${path}"`;

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

export function deactivate() {
  if (organizeFileCommand) {
    organizeFileCommand.dispose();
    organizeFileCommand = undefined;
  }
  if (organizeFolderCommand) {
    organizeFolderCommand.dispose();
    organizeFolderCommand = undefined;
  }
}
