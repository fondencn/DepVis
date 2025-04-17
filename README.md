# DepVis

DepVis is a dependency visualization tool for .NET projects. It scans a folder for `.dll` and `.exe` files, builds a dependency graph, and provides both XML and visual representations of the dependencies.

## Features

- Scans directories for `.dll` and `.exe` files.
- Detects and handles circular dependencies.
- Outputs the dependency graph as an XML file.
- Visualizes the dependency graph using Graphviz.

## Requirements

- .NET 8.0 SDK or later
- Graphviz installed and added to the system's PATH (for visualization)

### Installing Graphviz

To install Graphviz, you can use [Chocolatey](https://chocolatey.org/)
(a Windows package manager). 
Run the following command in an elevated command prompt (Administrator mode):
```
choco install graphviz -y
```

After installation, ensure that the `dot` executable is available in your system's PATH. You can verify this by running:
```
dot -V
```
This should display the version of Graphviz installed.

## Installation

1. Clone the repository:
   ```sh
   git clone https://github.com/yourusername/DepVis.git
   cd DepVis/src/DepVis
   ```

2. Restore dependencies:
   ```sh
   dotnet restore
   ```

3. Build the project:
   ```sh
   dotnet build
   ```

## Usage

1. Run the program with the folder path to scan:
   ```sh
   dotnet run -- <folderPath>
   ```

   Replace `<folderPath>` with the path to the folder containing `.dll` and `.exe` files.

2. The dependency graph will be saved as:
   - `DependencyGraph.xml` (XML format)
   - `DependencyGraph.dot` (Graphviz DOT format)

3. The dependency graph will be visualized using the graphviz `dot` command in
   the `DependencyGraph.png` file in the apps working dir.

## Example

```sh
dotnet run -- C:\MyProjects
```

## Project Structure

- `src/DepVis/Program.cs`: Main entry point of the application.
- `src/DepVis/FileDotEngine.cs`: Handles Graphviz DOT file generation.
- `src/DepVis/NativeMethods.cs`: Contains P/Invoke declarations for Windows API functions.
- `src/DepVis/DepVis.csproj`: Project file with dependencies and build configuration.

## Contributing

Contributions are welcome! Please fork the repository and submit a pull request.

## License

This project is licensed under the MIT License.
