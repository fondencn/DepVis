using QuickGraph;
using QuickGraph.Graphviz;

namespace DepVis
{
    internal static class Visualizer
    {


        private static async void MonitorProcessAsync(System.Diagnostics.Process process)
        {
            try
            {
                while (!process.HasExited)
                {
                    // Print CPU and memory usage
                    Console.WriteLine($"[dot process] running {(DateTime.Now - process.StartTime)} ...");
                    await Task.Delay(5000);

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error monitoring process: {ex.Message}");
            }
        }

        public static void VisualizeGraph(string folderPath, Dictionary<string, List<string>> dependencyGraph)
        {
            // Create a graph object using QuickGraph.
            var graph = new AdjacencyGraph<string, Edge<string>>();

            // Add vertices and edges to the graph.
            foreach (var kvp in dependencyGraph)
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
                        Arguments = $"-Tpng \"{outputPath}\" -v -o \"{pngFilePath}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };


                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Console.WriteLine($"[dot]: {e.Data}");
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Console.WriteLine($"[dot error]: {e.Data}");
                    }
                };
                Console.WriteLine($"Graph visualization will now be saved as PNG file to {pngFilePath}...");
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();


                // Start monitoring the process asynchronously
                MonitorProcessAsync(process);

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