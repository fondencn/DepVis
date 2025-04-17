using QuickGraph.Graphviz;
using QuickGraph.Graphviz.Dot;


namespace DepVis
{
    class FileDotEngine : IDotEngine
    {
        public string Run(GraphvizImageType imageType, string dot, string outputFileName)
        {
            // Write the DOT file to disk.
            File.WriteAllText(outputFileName, dot);
            return outputFileName;
        }
    }

}