using PeNet;
using System.IO;

namespace DepVis
{
    public class PFEHeaderDependencyAnalyzer : DependencyAnalyzer
    {
        public override void ExecuteDependencyCheck(string folderPath, bool recursíve)
        {
            // Set the root folder path for the scan
            _rootFolderPath = folderPath;
            _DependencyGraph.Clear();

            // Recursively scan the folder for .dll and .exe files
            var files = Directory.EnumerateFiles(folderPath, "*.*", recursíve ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                .Where(f => f.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));

            foreach (var file in files)
            {
                try
                {
                    // Analyze dependencies for each file
                    var dependencies = GetDependencies(file);
                    var moduleName = Path.GetFileName(file);
                    _DependencyGraph[moduleName] = dependencies;

                    // Log the results
                    Output?.AppendLine($"[info] File: {file}");
                    Output?.AppendLine($"[info] \t-->\t{dependencies.Count} dependencies");
                }
                catch (Exception ex)
                {
                    // Log any errors encountered while processing the file
                    Output?.AppendLine($"[error] Failed to analyze {file}: {ex.Message}");
                }
            }

            // Save the dependency graph to an XML file
            string xmlPath = Path.Combine(folderPath, "DependencyGraph.xml");
            SaveGraphAsXml(xmlPath);
            Output?.AppendLine($"Dependency graph saved to {xmlPath}");
        }

        private List<string> GetDependencies(string filePath)
        {
            var dependencies = new List<string>();

            try
            {
                // Use PeNet to parse the PE file
                var peFile = new PeFile(filePath);

                // Extract the list of imported DLLs
                dependencies = peFile.ImportedFunctions
                    .Select(f => f.DLL)
                    /* nur Dateien im Zielordner, keine Windows DLLs usw... */
                    .Where (dll => File.Exists(Path.Combine(_rootFolderPath, dll) ))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            catch (Exception ex)
            {
                // Log any errors encountered while parsing the file
                Output?.AppendLine($"[error] Failed to parse dependencies for {filePath}: {ex.Message}");
            }

            return dependencies;
        }
    }
}
