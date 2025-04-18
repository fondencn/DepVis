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
            Visualizer.VisualizeGraph(folderPath, DependencyGraph); // Visualize the dependency graph using QuickGraph and Graphviz.
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
                //Console.WriteLine($"Circular dependency detected: {filePath}");
                return;
            }

            // Skip files that have already been processed.
            if (ProcessedFiles.Contains(filePath))
                return;

            visited.Add(filePath); // Mark the file as visited for this recursion path.
            ProcessedFiles.Add(filePath); // Mark the file as processed globally.

            try
            {
                Console.WriteLine($"[info] processing file: {filePath}");
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

            // Suppress error message boxes
            uint previousErrorMode = NativeMethods.SetErrorMode(NativeMethods.SEM_FAILCRITICALERRORS | NativeMethods.SEM_NOOPENFILEERRORBOX);


            // Use LoadLibraryEx to load the file without resolving references.
            IntPtr hModule = NativeMethods.LoadLibraryEx(filePath, IntPtr.Zero, LoadLibraryFlags.DONT_RESOLVE_DLL_REFERENCES);
            if (hModule == IntPtr.Zero)
            {
                // If the file cannot be loaded, assume it is not a valid MFC DLL or EXE and skip it.
                NativeMethods.SetErrorMode(previousErrorMode); // Restore the previous error mode
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
                NativeMethods.SetErrorMode(previousErrorMode); // Restore the previous error mode

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
    }
}