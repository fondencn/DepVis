using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using QuickGraph;
using QuickGraph.Graphviz;


namespace DepVis
{
    public class Program
    {
        private static readonly HashSet<string> ProcessedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase); // Tracks files already processed to avoid redundant work.
        private static readonly Dictionary<string, List<string>> DependencyGraph = new Dictionary<string, List<string>>(); // Stores the dependency graph as a dictionary.

        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: DependencyGraphProgram <folderPath>");
                return;
            }

            string folderPath = args[0];
            if (!Directory.Exists(folderPath))
            {
                Console.WriteLine("Invalid folder path.");
                return;
            }

            BuildDependencyGraph(folderPath); // Build the dependency graph by scanning the folder.

            string xmlPath = Path.Combine(folderPath, "DependencyGraph.xml");
            SaveGraphAsXml(xmlPath); // Save the dependency graph to an XML file.
            Console.WriteLine($"Dependency graph saved to {xmlPath}");

            VisualizeGraph(folderPath); // Visualize the dependency graph using QuickGraph and Graphviz.
        }

        private static void BuildDependencyGraph(string folderPath)
        {
            // Recursively scan the folder for .dll and .exe files.
            foreach (var file in Directory.EnumerateFiles(folderPath, "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)))
            {
                ProcessFile(file, new HashSet<string>()); // Process each file and build its dependencies.
            }
        }

        private static void ProcessFile(string filePath, HashSet<string> visited)
        {
            // Detect circular dependencies by checking if the file is already in the visited set.
            if (visited.Contains(filePath))
            {
                Console.WriteLine($"Circular dependency detected: {filePath}");
                return;
            }

            // Skip files that have already been processed.
            if (ProcessedFiles.Contains(filePath))
                return;

            visited.Add(filePath); // Mark the file as visited for this recursion path.
            ProcessedFiles.Add(filePath); // Mark the file as processed globally.

            try
            {
                var dependencies = GetDependencies(filePath); // Get the dependencies of the current file.
                DependencyGraph[filePath] = dependencies; // Add the dependencies to the graph.

                // Recursively process each dependency.
                foreach (var dependency in dependencies)
                {
                    ProcessFile(dependency, new HashSet<string>(visited)); // Pass a copy of the visited set to avoid modifying the original.
                }
            }
            catch (Exception ex)
            {
                // Log any errors encountered while processing the file.
                Console.WriteLine($"Error processing {filePath}: {ex.Message}");
            }
        }

        private static List<string> GetDependencies(string filePath)
        {
            var dependencies = new List<string>();

            // Use LoadLibraryEx to load the file without resolving references.
            IntPtr hModule = NativeMethods.LoadLibraryEx(filePath, IntPtr.Zero, LoadLibraryFlags.DONT_RESOLVE_DLL_REFERENCES);
            if (hModule == IntPtr.Zero)
            {
                // If the file cannot be loaded, assume it is not a valid MFC DLL or EXE and skip it.
                return dependencies;
            }

            try
            {
                IntPtr[] modules = new IntPtr[1024]; // Buffer to hold module handles.
                GCHandle handle = GCHandle.Alloc(modules, GCHandleType.Pinned); // Pin the buffer in memory for the API call.
                try
                {
                    IntPtr modulesPtr = handle.AddrOfPinnedObject();
                    uint cbNeeded;

                    // Enumerate all modules loaded in the current process.
                    if (NativeMethods.EnumProcessModules(NativeMethods.GetCurrentProcess(), modulesPtr, (uint)(modules.Length * IntPtr.Size), out cbNeeded))
                    {
                        int moduleCount = (int)(cbNeeded / IntPtr.Size); // Calculate the number of modules returned.
                        for (int i = 0; i < moduleCount; i++)
                        {
                            var moduleHandle = modules[i];
                            var moduleName = new char[1024]; // Buffer to hold the module name.
                            uint size = NativeMethods.GetModuleFileNameEx(NativeMethods.GetCurrentProcess(), moduleHandle, moduleName, (uint)moduleName.Length);

                            if (size > 0)
                            {
                                // Convert the module name to a string and add it to the dependencies list.
                                string dependencyPath = new string(moduleName, 0, (int)size);
                                dependencies.Add(dependencyPath);
                            }
                        }
                    }
                }
                finally
                {
                    handle.Free(); // Free the pinned buffer.
                }
            }
            catch
            {
                // Ignore errors and assume invalid dependencies.
            }
            finally
            {
                NativeMethods.FreeLibrary(hModule); // Free the loaded module to release resources.
            }

            return dependencies;
        }

        private static void SaveGraphAsXml(string xmlPath)
        {
            // Save the dependency graph as an XML file.
            var xDoc = new XDocument(
                new XElement("DependencyGraph",
                    DependencyGraph.Select(kvp =>
                        new XElement("File",
                            new XAttribute("Path", kvp.Key),
                            kvp.Value.Select(dep => new XElement("Dependency", dep))))));

            xDoc.Save(xmlPath);
        }

        private static void VisualizeGraph(string folderPath)
        {
            // Create a graph object using QuickGraph.
            var graph = new AdjacencyGraph<string, Edge<string>>();

            // Add vertices and edges to the graph.
            foreach (var kvp in DependencyGraph)
            {
                foreach (var dependency in kvp.Value)
                {
                    graph.AddVerticesAndEdge(new Edge<string>(kvp.Key, dependency));
                }
            }

            // Generate a DOT file for visualization using Graphviz.
            var graphviz = new GraphvizAlgorithm<string, Edge<string>>(graph);
            string outputPath = Path.Combine(folderPath, "DependencyGraph.dot");
            graphviz.Generate(new FileDotEngine(), outputPath);
            Console.WriteLine($"Graph visualization saved to {outputPath}");


            // Generate a PNG file from the DOT file using Graphviz.
            string pngFilePath = Path.Combine(folderPath, "DependencyGraph.png");
            try
            {
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "dot", // Ensure Graphviz's 'dot' executable is in the system PATH.
                        Arguments = $"-Tpng \"{outputPath}\" -o \"{pngFilePath}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    Console.WriteLine($"Graph PNG visualization saved to {pngFilePath}");
                }
                else
                {
                    string error = process.StandardError.ReadToEnd();
                    Console.WriteLine($"Error generating PNG: {error}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to generate PNG file: {ex.Message}");
            }
        }
    }

}