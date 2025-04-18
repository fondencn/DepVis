using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Xml.Linq;

namespace DepVis
{
    internal class SymbolDependecyAnalyzer : DependencyAnalyzer
    {
        private  readonly HashSet<string> _ProcessedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase); // Tracks files already processed to avoid redundant work.


        public  void BuildDependencyGraph(string folderPath, bool recursíve)
        {
            _rootFolderPath = folderPath; // Set the root folder path for the scan.
            _ProcessedFiles.Clear();
            _DependencyGraph.Clear();
            // Recursively scan the folder for .dll and .exe files.
            foreach (var file in Directory.EnumerateFiles(folderPath, "*.*", recursíve ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly) // currently non-recursive
                .Where(f => f.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))) //
            {
                ProcessFile(file, new HashSet<string>()); // Process each file and build its dependencies.
            }
        }

        public override void ExecuteDependencyCheck(string folderPath, bool recursíve)
        {
            BuildDependencyGraph(folderPath, recursíve); // Build the dependency graph by scanning the folder.

            string xmlPath = Path.Combine(folderPath, "DependencyGraph.xml");
            SaveGraphAsXml(xmlPath); // Save the dependency graph to an XML file.
            Output?.AppendLine($"Dependency graph saved to {xmlPath}");
        }

        //private static List<string> GetDependencies(string filePath)
        //{
        //    var dependencies = new List<string>();

        //    // Suppress error message boxes
        //    uint previousErrorMode = NativeMethods.SetErrorMode(NativeMethods.SEM_FAILCRITICALERRORS | NativeMethods.SEM_NOOPENFILEERRORBOX);


        //    IntPtr hModule = NativeMethods.LoadLibraryEx(filePath, IntPtr.Zero, LoadLibraryFlags.NONE);
        //    if (hModule == IntPtr.Zero)
        //    {
        //        // If the file cannot be loaded, assume it is not a valid MFC DLL or EXE and skip it.
        //        NativeMethods.SetErrorMode(previousErrorMode); // Restore the previous error mode
        //        return dependencies;
        //    }

        //    try
        //    {
        //        IntPtr[] modules = new IntPtr[1024]; // Buffer to hold module handles.
        //        GCHandle handle = GCHandle.Alloc(modules, GCHandleType.Pinned); // Pin the buffer in memory for the API call.
        //        try
        //        {
        //            IntPtr modulesPtr = handle.AddrOfPinnedObject();
        //            uint cbNeeded;

        //            // Enumerate all modules loaded in the current process.
        //            if (NativeMethods.EnumProcessModulesEx(NativeMethods.GetCurrentProcess(), modulesPtr, (uint)(modules.Length * IntPtr.Size), out cbNeeded, NativeMethods.LIST_MODULES_ALL))
        //            {
        //                int moduleCount = (int)(cbNeeded / IntPtr.Size); // Calculate the number of modules returned.
        //                for (int i = 0; i < moduleCount; i++)
        //                {
        //                    var moduleHandle = modules[i];
        //                    var moduleName = new char[1024]; // Buffer to hold the module name.
        //                    uint size = NativeMethods.GetModuleFileNameEx(NativeMethods.GetCurrentProcess(), moduleHandle, moduleName, (uint)moduleName.Length);

        //                    if (size > 0)
        //                    {
        //                        // Convert the module name to a string and add it to the dependencies list.
        //                        string dependencyPath = new string(moduleName, 0, (int)size);
        //                        bool isOutside = Path.GetFullPath(Path.GetDirectoryName(Path.GetFullPath(dependencyPath))) != _rootFolderPath;
        //                        bool isSelf = dependencyPath == filePath;
        //                        if (!isOutside && !isSelf)
        //                        {
        //                            dependencies.Add(dependencyPath);
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //        finally
        //        {
        //            handle.Free(); // Free the pinned buffer.
        //        }
        //    }
        //    catch
        //    {
        //        // Ignore errors and assume invalid dependencies.
        //    }
        //    finally
        //    {
        //        NativeMethods.FreeLibrary(hModule); // Free the loaded module to release resources.
        //        NativeMethods.SetErrorMode(previousErrorMode); // Restore the previous error mode

        //    }

        //    return dependencies;
        //}
        private static List<string> GetDependencies(string filePath)
        {
            var dependencies = new List<string>();

            // Initialize the symbol handler
            if (!NativeMethods.SymInitialize(NativeMethods.GetCurrentProcess(), null, false))
            {
                int errorCode = Marshal.GetLastWin32Error();
                Output?.AppendLine($"[error] SymInitialize failed with error code: {errorCode}");
                return dependencies;
            }

            try
            {



                // Load the module
                ulong baseAddress = NativeMethods.SymLoadModuleEx(
                    NativeMethods.GetCurrentProcess(),
                    IntPtr.Zero,
                    filePath,
                    null,
                    0,
                    0,
                    IntPtr.Zero,
                    0);

                if (baseAddress == 0)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    Output?.AppendLine($"[error] SymLoadModuleEx failed for {filePath} with error code: {errorCode}");
                    return dependencies;
                }

                // Enumerate the import table
                if (!NativeMethods.SymEnumerateModules(
                        NativeMethods.GetCurrentProcess(),
                        (moduleName, baseOfDll, userContext) =>
                        {
                            if (!string.IsNullOrEmpty(moduleName) && !filePath.Contains(moduleName, StringComparison.OrdinalIgnoreCase))
                            {
                                dependencies.Add(moduleName);
                            }
                            return true; // Continue enumeration
                        },
                        IntPtr.Zero))
                {
                    Output?.AppendLine($"[error] Failed to enumerate modules for {filePath}");
                }
            }
            catch (Exception ex)
            {
                Output?.AppendLine($"[error] Failed to parse dependencies for {filePath}: {ex.Message}");
            }
            finally
            {
                // Cleanup the symbol handler
                NativeMethods.SymCleanup(NativeMethods.GetCurrentProcess());
            }

            return dependencies;
        }

        private static readonly string[] _filesToIgnore = new string[]
        {
            "capi2032.dll","cphmpro.dll"
        };

        private  void ProcessFile(string filePath, HashSet<string> visited)
        {
            // Detect circular dependencies by checking if the file is already in the visited set.
            if (visited.Contains(filePath))
            {
                //Console.WriteLine($"Circular dependency detected: {filePath}");
                return;
            }

            if(_filesToIgnore.Any(item => filePath.Contains(item, StringComparison.OrdinalIgnoreCase)))
            {
                // File Ignored -> Image cannot be loaded
                return;
            }

            // Skip files that have already been processed.
            if (_ProcessedFiles.Contains(filePath))
                return;

            visited.Add(filePath); // Mark the file as visited for this recursion path.

            // Skip files outside target folder
            if (Path.GetFullPath(Path.GetDirectoryName(Path.GetFullPath(filePath))) != _rootFolderPath)
            {
                return;
            }

            _ProcessedFiles.Add(filePath); // Mark the file as processed globally.

            try
            {
                Output?.AppendLine($"[info] file: {filePath}");
                var dependencies = GetDependencies(filePath); // Get the dependencies of the current file.
                _DependencyGraph[filePath] = dependencies; // Add the dependencies to the graph.

                Output?.AppendLine($"[info] \t-->\t{dependencies.Count} dependencies");

                // Recursively process each dependency.
                foreach (var dependency in dependencies)
                {
                    ProcessFile(dependency, new HashSet<string>(visited)); // Pass a copy of the visited set to avoid modifying the original.
                }
            }
            catch (Exception ex)
            {
                // Log any errors encountered while processing the file.
                Output?.AppendLine($"Error processing {filePath}: {ex.Message}");
            }
        }
    }
}