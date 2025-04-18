using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DepVis
{
    public  abstract class DependencyAnalyzer
    {
        protected readonly Dictionary<string, List<string>> _DependencyGraph = new Dictionary<string, List<string>>(); // Stores the dependency graph as a dictionary.
        public Dictionary<string, List<string>> DependencyGraph => _DependencyGraph; // Exposes the dependency graph for external access.

        protected static string _rootFolderPath = string.Empty; // Root folder path for the dependency scan.

        public static OutputViewModel? Output { get; internal set; }
        public abstract void ExecuteDependencyCheck(string folderPath);

        public static DependencyAnalyzer Create(DependencyAnalyzerMode mode)
        {
            if (mode == DependencyAnalyzerMode.DebugSymbols) 
            {   
                return new SymbolDependecyAnalyzer();
            }
            else if (mode == DependencyAnalyzerMode.PFEHeader)
            {
                return new PFEHeaderDependencyAnalyzer();
            }
            else
            {
                throw new ArgumentException("Invalid mode specified.");
            }
        }
    }

    public enum DependencyAnalyzerMode
    {
        DebugSymbols = 0, 
        PFEHeader = 1
    }
}
