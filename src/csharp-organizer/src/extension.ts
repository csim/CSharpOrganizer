import * as vscode from "vscode";
import * as cp from "child_process";
import * as path from "path";
import * as fs from "fs";

export function activate(context: vscode.ExtensionContext) {
  console.log("CSharp Organizer extension is now active!");

  // Register organize command
  let organizeDisposable = vscode.commands.registerCommand(
    "csharp-organizer.organize",
    async (uri: vscode.Uri) => {
      const targetUri = uri || vscode.window.activeTextEditor?.document.uri;
      if (targetUri) {
        await organizeFile(targetUri);
      }
    }
  );

  // Register organize all command
  let organizeAllDisposable = vscode.commands.registerCommand(
    "csharp-organizer.organizeAll",
    async (uri: vscode.Uri) => {
      if (uri) {
        await organizeDirectory(uri);
      }
    }
  );

  // Register on save handler
  let onSaveDisposable = vscode.workspace.onDidSaveTextDocument(
    async (document) => {
      const config = vscode.workspace.getConfiguration("csharpOrganizer");
      const organizeOnSave = config.get<boolean>("organizeOnSave", false);

      if (
        organizeOnSave &&
        document.languageId === "csharp" &&
        document.uri.scheme === "file"
      ) {
        await organizeFile(document.uri);
      }
    }
  );

  context.subscriptions.push(
    organizeDisposable,
    organizeAllDisposable,
    onSaveDisposable
  );
}

async function organizeFile(uri: vscode.Uri): Promise<void> {
  if (!uri || uri.scheme !== "file") {
    vscode.window.showErrorMessage("Invalid file path");
    return;
  }

  const filePath = uri.fsPath;
  if (!filePath.endsWith(".cs")) {
    vscode.window.showWarningMessage("Only C# files can be organized");
    return;
  }

  const config = vscode.workspace.getConfiguration("csharpOrganizer");
  const executablePath = config.get<string>(
    "executablePath",
    "csharp-organizer"
  );

  try {
    await vscode.window.withProgress(
      {
        location: vscode.ProgressLocation.Notification,
        title: "Organizing C# file...",
        cancellable: false,
      },
      async () => {
        await runOrganizer(executablePath, filePath);
      }
    );

    vscode.window.showInformationMessage(
      `Organized: ${path.basename(filePath)}`
    );

    // Refresh the file in the editor
    const document = await vscode.workspace.openTextDocument(uri);
    if (vscode.window.activeTextEditor?.document === document) {
      await vscode.commands.executeCommand("workbench.action.files.revert");
    }
  } catch (error) {
    const errorMessage =
      error instanceof Error ? error.message : "Unknown error";
    vscode.window.showErrorMessage(`Failed to organize file: ${errorMessage}`);
  }
}

async function organizeDirectory(uri: vscode.Uri): Promise<void> {
  const config = vscode.workspace.getConfiguration("csharpOrganizer");
  const executablePath = config.get<string>(
    "executablePath",
    "csharp-organizer"
  );

  try {
    const result = await vscode.window.withProgress(
      {
        location: vscode.ProgressLocation.Notification,
        title: "Organizing C# files in directory...",
        cancellable: false,
      },
      async () => {
        return await runOrganizer(executablePath, uri.fsPath);
      }
    );

    vscode.window.showInformationMessage(
      `Organized C# files in: ${path.basename(uri.fsPath)}`
    );
  } catch (error) {
    const errorMessage =
      error instanceof Error ? error.message : "Unknown error";
    vscode.window.showErrorMessage(
      `Failed to organize directory: ${errorMessage}`
    );
  }
}

function runOrganizer(
  executablePath: string,
  targetPath: string
): Promise<string> {
  return new Promise((resolve, reject) => {
    const args = [targetPath];

    const process = cp.spawn(executablePath, args, {
      cwd: vscode.workspace.workspaceFolders?.[0]?.uri.fsPath,
      shell: true,
    });

    let stdout = "";
    let stderr = "";

    process.stdout?.on("data", (data) => {
      stdout += data.toString();
    });

    process.stderr?.on("data", (data) => {
      stderr += data.toString();
    });

    process.on("close", (code) => {
      if (code === 0) {
        resolve(stdout);
      } else {
        reject(new Error(`Process exited with code ${code}: ${stderr}`));
      }
    });

    process.on("error", (error) => {
      reject(new Error(`Failed to start process: ${error.message}`));
    });
  });
}

export function deactivate() {}
