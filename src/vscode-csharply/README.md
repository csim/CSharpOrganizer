# CSharply

A Visual Studio Code extension for organizing C# files using the CSharply dotnet tool.

## Features

- **Organize File**: Organize the currently active C# file.
 
- **Organize Folder**: Organize all C# files in the current workspace folders.

## Requirements

The CSharply dotnet tool must be installed and available in your system PATH. 

To install: `dotnet tool install csharply --global`  

## Usage

### Organize Current File
1. Open a C# file (.cs)
2. Open Command Palette (`Ctrl+Shift+P`)
3. Run "CSharply: Organize File"

### Organize Workspace Folder
1. Open a workspace/folder containing C# files
2. Open Command Palette (`Ctrl+Shift+P`)
3. Run "CSharply: Organize Folder"

## Commands

- `csharply.organize.file` - Organize the current C# file
- `csharply.organize.workspacefolders` - Organize C# files in all workspace folders

## License

This extension is released under the [MIT License](LICENSE).
