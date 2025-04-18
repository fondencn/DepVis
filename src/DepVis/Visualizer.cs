using System.Drawing;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GraphX.Common.Enums;
using GraphX.Common.Models;
using GraphX.Controls;
using GraphX.Logic.Models;
using QuickGraph;

namespace DepVis
{
    internal static class Visualizer
    {

        public static OutputViewModel? Output { get; internal set; }

        public static void VisualizeGraph(string folderPath, Dictionary<string, List<string>> dependencyGraph)
        {
            // Create a graph object using QuickGraph.
            var graph = new BidirectionalGraph<DataVertex, DataEdge>();

            // Add vertices and edges to the graph.
            var vertices = new Dictionary<string, DataVertex>();
            foreach (var kvp in dependencyGraph)
            {
                if (!vertices.ContainsKey(kvp.Key))
                {
                    var vertex = new DataVertex(kvp.Key);
                    vertices[kvp.Key] = vertex;
                    graph.AddVertex(vertex);
                }

                foreach (var dependency in kvp.Value)
                {
                    if (!vertices.ContainsKey(dependency))
                    {
                        var vertex = new DataVertex(dependency);
                        vertices[dependency] = vertex;
                        graph.AddVertex(vertex);
                    }

                    var edge = new DataEdge(vertices[kvp.Key], vertices[dependency]);
                    graph.AddEdge(edge);
                }
            }

            // Create a GraphArea for rendering.
            var graphArea = new GraphArea<DataVertex, DataEdge, BidirectionalGraph<DataVertex, DataEdge>>
            {
                LogicCore = new GXLogicCore<DataVertex, DataEdge, BidirectionalGraph<DataVertex, DataEdge>>()
            };

            graphArea.LogicCore.Graph = graph;
            //The Kamada-Kawai (LayoutAlgorithmTypeEnum.KK) layout algorithm is computationally expensive for large graphs.
            //Switch to a faster algorithm like FR (Fruchterman-Reingold) or Tree if the graph structure allows it:
            //graphArea.LogicCore.DefaultLayoutAlgorithm = LayoutAlgorithmTypeEnum.KK; // Use Kamada-Kawai layout
            graphArea.LogicCore.DefaultLayoutAlgorithm = LayoutAlgorithmTypeEnum.FR; // Use Kamada-Kawai layout
            //The FSA (Force-Scan Algorithm) for overlap removal can be replaced with a simpler algorithm
            //like None if overlaps are acceptable:
            //graphArea.LogicCore.DefaultOverlapRemovalAlgorithm = OverlapRemovalAlgorithmTypeEnum.FSA;
            graphArea.LogicCore.DefaultOverlapRemovalAlgorithm = OverlapRemovalAlgorithmTypeEnum.None;
            //The SimpleER (Simple Edge Routing) algorithm can be replaced with None to skip edge routing entirely:
            //graphArea.LogicCore.DefaultEdgeRoutingAlgorithm = EdgeRoutingAlgorithmTypeEnum.SimpleER;
            graphArea.LogicCore.DefaultEdgeRoutingAlgorithm = EdgeRoutingAlgorithmTypeEnum.None;
            //The FSA (Force-Scan Algorithm) for overlap removal can be replaced with a simpler
            //algorithm like None if overlaps are acceptable:
            graphArea.LogicCore.DefaultOverlapRemovalAlgorithm = OverlapRemovalAlgorithmTypeEnum.None;

            graphArea.LogicCore.AsyncAlgorithmCompute = true;

            // Generate the graph layout.
            graphArea.GenerateGraph(true);

            // Render the graph to an image.
            string pngFilePath = Path.Combine(folderPath, "DependencyGraph.png");
            SaveGraphAsImage(graphArea, pngFilePath);

            Output?.AppendLine($"Graph visualization saved to {pngFilePath}");
        }

        private static void SaveGraphAsImage(GraphArea<DataVertex, DataEdge, BidirectionalGraph<DataVertex, DataEdge>> graphArea, string filePath)
        {
            // Measure and arrange the graph area.
            graphArea.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
            graphArea.Arrange(new Rect(graphArea.DesiredSize));

            // Render the graph area to a bitmap.
            var renderTarget = new RenderTargetBitmap((int)graphArea.ActualWidth, (int)graphArea.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            renderTarget.Render(graphArea);

            // Save the bitmap as a PNG file.
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderTarget));
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                encoder.Save(stream);
            }
        }
    }

    // DataVertex and DataEdge classes for GraphX
    public class DataVertex : VertexBase
    {
        public string Text { get; set; }

        public DataVertex(string text = "")
        {
            Text = text;
        }

        public override string ToString()
        {
            return Text;
        }
    }

    public class DataEdge : EdgeBase<DataVertex>
    {
        public DataEdge(DataVertex source, DataVertex target, double weight = 1)
            : base(source, target, weight)
        {
        }

        public override string ToString()
        {
            return $"{Source} -> {Target}";
        }
    }
}