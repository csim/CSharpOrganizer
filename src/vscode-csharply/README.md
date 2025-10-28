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
3. Run "CSharply: Organize C# File"

### Organize Workspace Folder
1. Open a workspace/folder containing C# files
2. Open Command Palette (`Ctrl+Shift+P`)
3. Run "CSharply: Organize all C# files in workspace folders"

## Commands

- `csharply.organize.file` - Organize open C# file
- `csharply.organize.workspacefolders` - Organize all C# files in workspace folders

## License

This extension is released under the [MIT License](LICENSE).
