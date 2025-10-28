# CSharply

A Visual Studio Code extension for organizing C# files using the CSharply tool.

## Features

- **Organize File**: Organize the currently active C# file
- **Organize Folder**: Organize all C# files in the current workspace

## Requirements

- CSharply.exe must be installed and available in your system PATH. Install: `dotnet tool install csharply --global`  

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
- `csharply.organize.folder` - Organize all C# files in the workspace

## Release Notes

### 0.0.1

Initial release of CSharply extension.

## License

This extension is released under the [MIT License](LICENSE).
