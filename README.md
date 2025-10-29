# CSharply

[![build](https://github.com/csim/CSharply/actions/workflows/build.yml/badge.svg)](https://github.com/csim/CSharply/actions/workflows/build.yml)
[![nuget](https://img.shields.io/nuget/v/CSharply.svg)](https://www.nuget.org/packages/CSharply/)
[![vsCode](https://img.shields.io/visual-studio-marketplace/v/csim.csharply.svg)](https://marketplace.visualstudio.com/items?itemName=csim.csharply)

A powerful C# code organization tool that automatically organizes `using` statements and sorts class members according to best practices. Available as a CLI tool, web API, and VS Code extension.

## Features

- 🎯 **Automatic Code Organization**: Organizes using statements and class members
- 🔧 **Multiple Interfaces**: CLI, Web API, Named Pipes, and VS Code extension
- ⚡ **Fast Performance**: Built with Roslyn for accurate C# parsing
- 🌐 **Cross-Platform**: Works on Windows, macOS, and Linux
- 📁 **Batch Processing**: Organize entire directories or single files
- 🔄 **Real-time Processing**: Daemon service for persistent operations

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
- `GET /info` - Server information
- `POST /organize` - Organize C# code (plain text body)

#### Example API Usage

```bash
# Organize code via plain text
curl -X POST http://localhost:8147/organize \
  -H "Content-Type: text/plain" \
  -d "using System.Linq; using System; class Test { }"
```

The daemon service allows other processes to send C# code for organization via named pipes, useful for editor integrations and automated workflows.

### VS Code Extension

The VS Code extension provides:
- **Command**: "CSharply: Organize File" (`Ctrl+Shift+P`)
- **Context Menu**: Right-click any C# file
- **Automatic Integration**: Works with the daemon service if running

## What Gets Organized

CSharply organizes your C# code according to Microsoft's coding conventions:

### Using Statements
- Removes unused using statements
- Sorts alphabetically
- Groups System namespaces first
- Removes duplicates

### Class Members

Orders members by type and access modifier:
  1. Fields (private, protected, public)
  2. Constructors
  3. Properties
  4. Events
  5. Methods
  6. Nested types

### Example

**Before:**
```csharp
using System.Linq;
using System.Collections.Generic;
using System;

namespace MyProject
{
    public class Example
    {
        public void DoSomething() { }
        private string _field;
        public Example() { }
        public string Property { get; set; }
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
        private string _field;
        
        public Example() { }
        
        public string Property { get; set; }
        
        public void DoSomething() { }
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
