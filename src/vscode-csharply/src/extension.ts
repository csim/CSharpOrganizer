import * as vscode from "vscode";
import * as server from "./server";
import * as cli from "./cli";
import { getOutputChannel, log, showOutput } from "./utils";

let organizeFileCommand: vscode.Disposable | undefined;
let organizeFolderCommand: vscode.Disposable | undefined;

export function activate(context: vscode.ExtensionContext) {
  // Create the output channel when extension activates
  const outputChannel = getOutputChannel();
  context.subscriptions.push(outputChannel);

  // Log activation
  log("CSharply extension activating...");

  organizeFileCommand = vscode.commands.registerCommand(
    "csharply.organize.file",
    server.organizeFileCommand
  );

  organizeFolderCommand = vscode.commands.registerCommand(
    "csharply.organize.workspacefolders",
    cli.organizeWorkspaceFoldersCommand
  );

  const showOutputCommand = vscode.commands.registerCommand(
    "csharply.showOutput",
    () => {
      showOutput();
    }
  );

  const testServerCommand = vscode.commands.registerCommand(
    "csharply.test.server",
    async () => {
      log("Testing server connection...");
      const isConnected = await server.testConnection();
      const message = `Server connection: ${isConnected ? "OK" : "Failed"}`;
      log(message);
      vscode.window.showInformationMessage(message);
    }
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
    showOutputCommand,
    testServerCommand,
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
