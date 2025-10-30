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

  context.subscriptions.push(organizeFileCommand, organizeFolderCommand);

  log("CSharply extension activated.");
}

export function deactivate() {
  log("CSharply extension deactivating...");

  try {
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
