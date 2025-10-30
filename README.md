# CSharply
[![build](https://github.com/csim/CSharply/actions/workflows/build.yml/badge.svg)](https://github.com/csim/CSharply/actions/workflows/build.yml)
[![nuget](https://img.shields.io/nuget/v/CSharply.svg)](https://www.nuget.org/packages/CSharply/)
[![vsCode](https://img.shields.io/visual-studio-marketplace/v/csim.csharply.svg)](https://marketplace.visualstudio.com/items?itemName=csim.csharply)

An opinionated tool that organizes C# files according to best practices. Available as a CLI tool, web API, and VS Code extension.

## Features

- 🎯 **Automatic Code Organization**: Organizes all members and using statements
- 🔧 **Multiple Interfaces**: CLI, Web API, and VS Code extension
- ⚡ **Fast Performance**: Built with Roslyn for accurate C# parsing
- 🌐 **Cross-Platform**: Works on Windows, macOS, and Linux
- 📁 **Batch Processing**: Organize entire directories or single files
- 🔄 **Real-time Processing**: Web server for fast operations without startup costs per request

## Installation

### CLI Tool (Global)
```bash
dotnet tool install --global CSharply
```

### VS Code Extension
Install from the [Visual Studio Marketplace](https://marketplace.visualstudio.com/items?itemName=csim.csharply) or search for "CSharply" in VS Code extensions.


## Usage

### Command Line Interface

#### Organize a single file
```bash
csharply organize MyClass.cs
```

#### Organize an entire directory
```bash
csharply organize ./src
```

#### Get help
```bash
csharply --help
csharply organize --help
```

### Web Server Mode

Start a web server to organize code via HTTP API:

```bash
csharply serve
csharply serve --port 8149
```

#### API Endpoints

- `GET /health` - Health check
- `POST /organize` - Organize C# code (plain text body)

#### Example API Usage

```bash
# Organize code via plain text
curl -X POST http://localhost:8149/organize \
  -H "Content-Type: text/plain" \
  -d "using System.Linq; using System; class Test { }"
```

### VS Code Extension

`Ctrl+Shift+P`, then `CSharply: Organize C# file` or `CSharply: Organize all C# files in workspace folders`

## What Gets Organized

CSharply organizes your C# code according to Microsoft's coding conventions:

### Using Statements
- Sorts alphabetically
- Groups System namespaces first

### Rules

Member Order:
  1. Namespaces
  2. Interfaces
  3. Fields
  4. Properties
  5. Constructors
  6. Methods
  7. Nested types
  8. Enums

Access Modifier Order:
  1. public
  2. internal
  3. protected
  4. private

Note: If the file contains pre-processor directives such as `#if` or `#region`, the file will not be organized.


### Example

**Before:**
```csharp
using System.Collections.Generic;

using System.Linq;
using System;

namespace MyProject
{
    public class Example
    {
        public void DoSomething1() { }
        private string _field1;
        public void DoSomething2() { }
        public Example() { }
        public string Property { get; set; }
        private string _field2;
    }
}
```

**After:**
```csharp
using System;
using System.Collections.Generic;
using System.Linq;

namespace MyProject
{
    public class Example
    {
        private string _field1;
        private string _field2;
        
        public Example() { }
        
        public string Property { get; set; }
        
        public void DoSomething1() { }

        public void DoSomething2() { }
    }
}
```

## Ignoring Files

CSharply supports a `.csharplyignore` file to exclude specific files and directories from organization. This is useful for:

- Generated code files
- Third-party libraries
- Legacy code that shouldn't be modified
- Files with custom formatting requirements

### Creating a .csharplyignore File

Create a `.csharplyignore` file in your project root or any directory you want to organize. The file uses gitignore-style patterns:

```gitignore
# Ignore all generated files
**/Generated/
**/*.Designer.cs
**/*.g.cs

# Ignore specific files
Models/LegacyModel.cs
Controllers/ThirdPartyController.cs

# Ignore by pattern
**/*Template*.cs
**/Migrations/**/*.cs

# Ignore entire directories
bin/
obj/
packages/
```

### Pattern Syntax

The `.csharplyignore` file supports the same globbing patterns as `.gitignore`:

| Pattern | Description | Example |
|---------|-------------|---------|
| `*.cs` | Match any .cs file | `Generated.cs`, `Model.cs` |
| `**/Generated/` | Match Generated directory anywhere | `src/Generated/`, `test/Generated/` |
| `Models/*.cs` | Match .cs files in Models directory | `Models/User.cs` |
| `**/*.Designer.cs` | Match Designer.cs files anywhere | `Form1.Designer.cs` |
| `!Important.cs` | Negation - don't ignore this file | Override previous ignore rules |

### How It Works

1. CSharply looks for `.csharplyignore` files starting from the target directory
2. It walks up the directory tree to find additional ignore files
3. Patterns are applied in order, with more specific files taking precedence
4. Files matching any pattern are skipped during organization

### Examples

#### Basic Project Structure
```
MyProject/
├── .csharplyignore          # Root ignore file
├── src/
│   ├── .csharplyignore      # Source-specific ignores
│   ├── Controllers/
│   ├── Models/
│   └── Generated/           # Ignored directory
├── tests/
└── bin/                     # Ignored directory
```

#### Sample .csharplyignore for ASP.NET Core
```gitignore
# Build outputs
bin/
obj/
publish/

# Generated files
**/*.Designer.cs
**/*.g.cs
**/Migrations/*.cs

# Third-party code
**/ThirdParty/
**/External/
```

#### Sample .csharplyignore for WinForms/WPF
```gitignore
# Designer files
**/*.Designer.cs
**/*.g.cs

# Build outputs
bin/
obj/

# Legacy code
**/Legacy/
**/Old/
```

### Verbose Output

Use the `--verbose` flag to see which files are being ignored:

```bash
csharply organize ./src --verbose
# Output will show:
# skipped   : src/Generated/Model.cs
# organized : src/Controllers/UserController.cs
```

# Command Line Options

```bash
csharply organize [options] <path>

Options:
  --simulate, -s    Simulate changes without writing files
  --verbose, -v     Enable verbose output
  --help, -h        Show help information

csharply server [options]

Options:
  --port <port>     Port to listen on (default: 8149)
  --help, -h        Show help information
```
