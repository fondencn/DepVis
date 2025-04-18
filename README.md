# DepVis

DepVis is a powerful dependency visualization tool designed for native Windows / Microsoft Visual C++ libraries and executables. It scans directories for `.dll` and `.exe` files, builds a comprehensive dependency graph, and provides both XML and visual representations of the dependencies.

## Features

- Scans directories for `.dll` and `.exe` files.
- Detects and handles circular dependencies.
- Outputs the dependency graph in multiple formats:
  - XML (`DependencyGraph.xml`)
- Visualizes the dependency graph as an image (`DependencyGraph.png`) using Graphviz.
- Supports recursive scanning of directories for dependencies.
- Ignores specific files or directories based on predefined rules.

## Requirements

- .NET 8.0 SDK or later


## Installation

1. Clone the repository:
      git clone https://github.com/yourusername/DepVis.git
   cd DepVis/src/DepVis
   
2. Restore dependencies:
      dotnet restore
   
3. Build the project:
      dotnet build
   
## Usage

1. Run the program with the folder path to scan:
      dotnet run -- <folderPath> [--recursive]
      - Replace `<folderPath>` with the path to the folder containing `.dll` and `.exe` files.
   - Use the `--recursive` flag to enable recursive scanning of subdirectories.

2. The dependency graph will be saved as:
   - `DependencyGraph.xml` (XML format)

3. The dependency graph will be shown as window


## Example

dotnet run -- C:\MyProjects

## Project Structure

- `src/DepVis/Program.cs`: Main entry point of the application.
- `src/DepVis/FileDotEngine.cs`: Handles Graphviz DOT file generation.
- `src/DepVis/NativeMethods.cs`: Contains P/Invoke declarations for Windows API functions.
- `src/DepVis/SymbolDependencyAnalyzer.cs`: Implements recursive dependency scanning.
- `src/DepVis/DependencyAnalyzer.cs`: Base class for dependency analysis.
- `src/DepVis/PFEHeaderDependencyAnalyzer.cs`: Specialized analyzer for PFE headers.
- `src/DepVis/DepVis.csproj`: Project file with dependencies and build configuration.

## Contributing

Contributions are welcome! To contribute:

1. Fork the repository.
2. Create a new branch for your feature or bug fix.
3. Commit your changes and push
