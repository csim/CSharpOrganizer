import * as vscode from "vscode";
import * as server from "./server";
import * as cli from "./cli";
import { getOutputChannel, log, showOutput } from "./utils";

let organizeFileCommand: vscode.Disposable | undefined;
let organizeFolderCommand: vscode.Disposable | undefined;

export function activate(context: vscode.ExtensionContext) {
  const outputChannel = getOutputChannel();
  context.subscriptions.push(outputChannel);

  log("CSharply extension activating...");

  organizeFileCommand = vscode.commands.registerCommand(
    "csharply.organize.file",
    server.organizeFileCommand
  );

  organizeFolderCommand = vscode.commands.registerCommand(
    "csharply.organize.workspacefolders",
    cli.organizeWorkspaceFoldersCommand
  );

  const restartServerCommand = vscode.commands.registerCommand(
    "csharply.restart.server",
    async () => {
      try {
        log("Restarting server...");
        await server.restart();
        log("Server restarted successfully");
        vscode.window.showInformationMessage("Server restarted successfully");
      } catch (error) {
        const errorMsg = `Failed to restart server: ${error}`;
        log(errorMsg);
        vscode.window.showErrorMessage(errorMsg);
      }
    }
  );

  context.subscriptions.push(
    organizeFileCommand,
    organizeFolderCommand,
    restartServerCommand
  );

  log("CSharply extension activated.");
}

export function deactivate() {
  log("CSharply extension deactivating...");

  try {
    // Cleanup commands
    if (organizeFileCommand) {
      organizeFileCommand.dispose();
      organizeFileCommand = undefined;
    }

    if (organizeFolderCommand) {
      organizeFolderCommand.dispose();
      organizeFolderCommand = undefined;
    }

    cleanupProcesses();

    log("CSharply extension deactivated successfully");
  } catch (error) {
    log(`Error during deactivation: ${error}`);
  }
}

function cleanupProcesses() {
  log("Cleaning up processes...");

  server.stop();
}
