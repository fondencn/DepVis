using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DepVis
{
    /// <summary>
    /// Interaction logic for MainWin.xaml
    /// </summary>
    public partial class MainWin : Window
    {
        public MainWin()
        {
            InitializeComponent();
            this.Loaded += MainWin_Loaded;
        }
        private MainViewModel ViewModel => (MainViewModel)this.DataContext;
        private void MainWin_Loaded(object sender, RoutedEventArgs e)
        {
            this.ViewModel.DependencyGraphCreated += ViewModel_DependencyGraphCreated;
        }

        private void ViewModel_DependencyGraphCreated(object? sender, EventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                this.treeContainer.Content = null; // Clear the previous content
                this.treeContainer.Content = CreateTreeViewFromDependencyGraph(this.ViewModel.DependencyAnalyzer.DependencyGraph, this.ViewModel.Filter);
            });
            
        }

        public static TreeView CreateTreeViewFromDependencyGraph(Dictionary<string, List<string>> dependencyGraph, string? filter)
        {
            // Step 1: Identify root nodes (nodes that are not dependencies of any other node).
            HashSet<string> allNodes;
            if (!String.IsNullOrEmpty(filter))
            {
                allNodes = dependencyGraph.Keys
                    .Where(item => item.Contains(filter, StringComparison.OrdinalIgnoreCase))
                    .ToHashSet();
            }
            else
            {
                allNodes = dependencyGraph.Keys.ToHashSet();
            }
            HashSet<string> dependentNodes;
            if (!String.IsNullOrEmpty(filter))
            {

                dependentNodes = dependencyGraph.Values.SelectMany(deps => deps)
                    .Where(item => item.Contains(filter, StringComparison.OrdinalIgnoreCase))
                    .ToHashSet();
            }
            else
            {
                dependentNodes = dependencyGraph.Values.SelectMany(deps => deps).ToHashSet();
            }
            var rootNodes = dependencyGraph
                    .Where(item => item.Key.Contains(filter ?? "", StringComparison.OrdinalIgnoreCase))
                    .Where(item => item.Value.Count != 0)
                    .ToHashSet();

            // Step 2: Create a TreeView and populate it with root nodes and their dependencies.
            var treeView = new TreeView();

            List<TreeViewItem> rootItems = new List<TreeViewItem>();
            foreach (var rootNode in rootNodes)
            {
                 rootItems.Add(CreateTreeViewItem(rootNode.Key, dependencyGraph));
            }
            foreach (var item in rootItems.OrderBy(item => item.Header)) 
            {
                treeView.Items.Add(item);
            }

            return treeView;
        }

        private static TreeViewItem CreateTreeViewItem(string node, Dictionary<string, List<string>> dependencyGraph)
        {
            // Create a TreeViewItem for the current node.
            var treeViewItem = new TreeViewItem
            {
                Header = node
            };

            // Check if the node has dependencies.
            if (dependencyGraph.TryGetValue(node, out var dependencies))
            {
                foreach (var dependency in dependencies)
                {
                    // Recursively create TreeViewItems for dependencies.
                    var childItem = CreateTreeViewItem(dependency, dependencyGraph);
                    treeViewItem.Items.Add(childItem);
                }
                treeViewItem.Header = $"[{dependencies.Count}] {treeViewItem.Header}";
            }
            else
            {
                treeViewItem.Header = $"[0] {treeViewItem.Header}";
            }

             return treeViewItem;
        }
    }
}
