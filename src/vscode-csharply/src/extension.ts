import * as vscode from "vscode";
import * as daemon from "./daemon";
import * as cli from "./cli";

let organizeFileCommand: vscode.Disposable | undefined;
let organizeFolderCommand: vscode.Disposable | undefined;

export function activate(context: vscode.ExtensionContext) {
  //console.log('Congratulations, your extension "csharply" is now active!');

  organizeFileCommand = vscode.commands.registerCommand(
    "csharply.organize.file",
    cli.organizeFileCommand
  );

  organizeFolderCommand = vscode.commands.registerCommand(
    "csharply.organize.workspacefolders",
    cli.organizeWorkspaceFoldersCommand
  );

  context.subscriptions.push(organizeFileCommand);
  context.subscriptions.push(organizeFolderCommand);
}

export function deactivate() {
  if (organizeFileCommand) {
    organizeFileCommand.dispose();
    organizeFileCommand = undefined;
  }

  daemon.stop();
}
