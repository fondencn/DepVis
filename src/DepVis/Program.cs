using System.IO;
using System.Runtime.InteropServices;
using System.Xml.Linq;


namespace DepVis
{
    public class Program
    {

        [STAThread]
        static void Main(string[] args)
        {
            // Initialize the application and set up the main window.
            var app = new System.Windows.Application();
            var mainWindow = new MainWin();
            var viewModel = new MainViewModel(args);
            mainWindow.DataContext = viewModel;
            mainWindow.Show();
            // Execute the dependency check if a folder path is provided.
            if (args.Length > 0)
            {
                viewModel.ExecuteScan(args[0]);
            }
            // Start the application event loop.
            app.Run(mainWindow);
        }
    }
}