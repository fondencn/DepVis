using System.Drawing;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GraphX.Common.Enums;
using GraphX.Common.Models;
using GraphX.Controls;
using GraphX.Logic.Algorithms.LayoutAlgorithms;
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

            // Configure the layout algorithm.
            graphArea.LogicCore.DefaultLayoutAlgorithm = LayoutAlgorithmTypeEnum.FR; 
            graphArea.LogicCore.DefaultOverlapRemovalAlgorithm = OverlapRemovalAlgorithmTypeEnum.FSA; // Force-Scan Algorithm
            graphArea.LogicCore.DefaultEdgeRoutingAlgorithm = EdgeRoutingAlgorithmTypeEnum.Bundling; // Simple Edge Routing
            graphArea.LogicCore.AsyncAlgorithmCompute = false;

            // Set layout algorithm parameters (e.g., spacing).
            //var layoutParameters = graphArea.LogicCore.AlgorithmFactory.CreateLayoutParameters(LayoutAlgorithmTypeEnum.FR);
            //if (layoutParameters is FreeFRLayoutParameters frParams)
            //{
            //    frParams.IterationLimit = 1000; // Increase iterations for better distribution
            //    frParams.AttractionMultiplier = 1.2; // Adjust attraction force
            //    frParams.RepulsiveMultiplier = 1.0; // Adjust repulsion force
            //    graphArea.LogicCore.DefaultLayoutAlgorithmParams = frParams;
            //}
            // Configure layout parameters for EfficientSugiyama
            //var layoutParameters = graphArea.LogicCore.AlgorithmFactory.CreateLayoutParameters(LayoutAlgorithmTypeEnum.EfficientSugiyama);
            //if (layoutParameters is SugiyamaLayoutParameters sugiyamaParams)
            //{
            //    sugiyamaParams.HorizontalGap = 200; 
            //    sugiyamaParams.VerticalGap = 200;
            //    sugiyamaParams.Simplify = true;
            //    sugiyamaParams.PositionCalculationMethod = PositionCalculationMethodTypes.PositionBased;
            //    graphArea.LogicCore.DefaultLayoutAlgorithmParams = sugiyamaParams;
            //}

            // Generate the graph layout.
            graphArea.GenerateGraph(true);

            double width = graphArea.VertexList.Values.Max(v => v.GetPosition().X) + 200; // Add padding
            double height = graphArea.VertexList.Values.Max(v => v.GetPosition().Y) + 200; // Add padding
            graphArea.Width = Math.Max(width, 1200); // Minimum width
            graphArea.Height = Math.Max(height, 800); // Minimum height

            graphArea.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
            graphArea.Arrange(new Rect(0, 0, graphArea.Width, graphArea.Height));


            graphArea.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
            graphArea.Arrange(new Rect(0, 0, graphArea.Width, graphArea.Height));


            // Debug: Log vertex positions.
            //foreach (var vertex in graphArea.VertexList)
            //{
            //    Output?.AppendLine($"Vertex {vertex.Key.Text} position: {vertex.Value.GetPosition().X}, {vertex.Value.GetPosition().Y}");
            //}

            // Dynamically adjust the GraphArea size based on the layout.
            //double width = 10000;
            //double height = 10000;

            //graphArea.Width = width;
            //graphArea.Height = height;

            // Render the graph to an image.
            //try
            //{
            //    string pngFilePath = Path.Combine(folderPath, "DependencyGraph.png");
            //    SaveGraphAsImage(graphArea, pngFilePath, (int)width, (int)height);
            //    Output?.AppendLine($"Graph visualization saved to {pngFilePath}");
            //}
            //catch (Exception ex)
            //{
            //    Output?.AppendLine($"Error saving graph image: {ex.Message}");
            //}
            Output?.AppendLine($"Graph visualization showing as new window...");
            var window = new Window
            {
                Title = $"{folderPath } Dependency Graph",
                Content = new ScrollViewer() { Content = graphArea, HorizontalScrollBarVisibility = ScrollBarVisibility.Visible, VerticalScrollBarVisibility = ScrollBarVisibility.Visible },
                Width = 1200,
                Height = 800
            };
            window.Show();

        }

        private static void SaveGraphAsImage(GraphArea<DataVertex, DataEdge, BidirectionalGraph<DataVertex, DataEdge>> graphArea, string filePath, int imageWidth, int imageHeight)
        {

            // Create a DrawingVisual to draw the background and the graph.
            var drawingVisual = new DrawingVisual();
            using (var drawingContext = drawingVisual.RenderOpen())
            {
                // Draw a grey background.
                drawingContext.DrawRectangle(Brushes.LightGray, null, new Rect(0, 0, imageWidth, imageHeight));

                // Measure and arrange the graph area.
                graphArea.Measure(new System.Windows.Size(imageWidth, imageHeight));
                graphArea.Arrange(new Rect(0, 0, imageWidth, imageHeight));

                // Render the graph area onto the DrawingContext.
                drawingContext.DrawRectangle(new VisualBrush(graphArea), null, new Rect(0, 0, imageWidth, imageHeight));
            }

            // Render the DrawingVisual to a RenderTargetBitmap.
            var renderTarget = new RenderTargetBitmap(imageWidth, imageHeight, 96, 96, PixelFormats.Pbgra32);
            renderTarget.Render(drawingVisual);

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