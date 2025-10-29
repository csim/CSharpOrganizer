# CSharply

[![build](https://github.com/csim/CSharply/actions/workflows/build.yml/badge.svg)](https://github.com/csim/CSharply/actions/workflows/build.yml)
[![nuget](https://img.shields.io/nuget/v/CSharply.svg)](https://www.nuget.org/packages/CSharply/)
[![vsCode](https://img.shields.io/visual-studio-marketplace/v/csim.csharply.svg)](https://marketplace.visualstudio.com/items?itemName=csim.csharply)

A opinionated C# code organization tool that automatically organizes C# files according to best practices. Available as a CLI tool, web API, and VS Code extension.

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
csharply serve --port 8080
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

### Class Members

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

## Configuration

### Command Line Options

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
