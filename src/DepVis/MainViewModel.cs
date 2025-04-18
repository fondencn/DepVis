using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DepVis
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public MainViewModel(string[] commandLineArgs)
        {
            if(commandLineArgs.Length > 0)
            {
                Path = commandLineArgs[0];
                if (!Directory.Exists(Path))
                {
                    Output.AppendLine("Invalid folder path.");
                }
            }
        }

        public OutputViewModel Output { get; } = new OutputViewModel();

        private string _path = string.Empty;
        public string Path
        {
            get => _path;
            set => SetProperty(ref _path, value);
        }

        public DependencyAnalyzerMode Mode { get; set; } = DependencyAnalyzerMode.DebugSymbols;

        public DependencyAnalyzer DependecyAnalyzer { get; set; } = DependencyAnalyzer.Create(DependencyAnalyzerMode.DebugSymbols);

        private ActionCommand? _ExecuteCommand = null;
        public ICommand ExecuteCommand
        {
            get
            {
                if (_ExecuteCommand == null)
                {
                    _ExecuteCommand = new ActionCommand(ExecuteScan);
                }
                return _ExecuteCommand;
            }
        }

        public void ExecuteScan(object? obj)
        {
            Output.Clear();
            Task.Run(() => ExecuteScanAsync()).ConfigureAwait(false); // Run the scan asynchronously
        }

        private void ExecuteScanAsync()
        {

            if (string.IsNullOrEmpty(Path))
            {
                Output.AppendLine("Please select a folder.");

                return;
            }
            if (!System.IO.Directory.Exists(Path))
            {
                Output.AppendLine("Invalid folder path.");
                return;
            }
            try
            {
                Output.AppendLine("Scan started.");
                DependencyAnalyzer.Output = this.Output;
                Visualizer.Output = this.Output;
                this.DependecyAnalyzer.ExecuteDependencyCheck(Path);
                Output.AppendLine("Dependency graph generated successfully.");
            }
            catch (Exception ex)
            {
                Output.AppendLine($"Error: {ex.Message}");
            }
        }


        private ActionCommand? _VisualizeCommand = null;
        public ICommand VisualizeCommand
        {
            get
            {
                if (_VisualizeCommand == null)
                {
                    _VisualizeCommand = new ActionCommand(Visualize);
                }
                return _VisualizeCommand;
            }
        }

        public void Visualize(object? obj)
        {
            Visualizer.VisualizeGraph(this.Path, this.DependecyAnalyzer.DependencyGraph);
        }



        private void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return;
            field = value;
            OnPropertyChanged(propertyName);
        }
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public event PropertyChangedEventHandler? PropertyChanged;



    }

    public class ActionCommand : ICommand
    {
        public ActionCommand(Action<object?> action)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
        }
        private readonly Action<object?> _action;
        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter)
        {
            _action(parameter);
        }
    }

    public class OutputViewModel : INotifyPropertyChanged
    {

        private StringBuilder _output = new StringBuilder();
        public string Output => _output.ToString();

        public void AppendLine(string line)
        {
            _output.AppendLine(DateTime.Now.ToString("HH:mm:ss") + "\t" + line);
            OnPropertyChanged(nameof(Output));
        }


        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        internal void Clear()
        {
            _output.Clear();
            OnPropertyChanged(nameof(Output));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
