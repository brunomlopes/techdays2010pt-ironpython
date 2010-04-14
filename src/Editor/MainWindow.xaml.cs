using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Xml;
using Editor.Model;
using ICSharpCode.AvalonEdit.Highlighting;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using NLog;
using NLog.Config;
using Path = System.IO.Path;

namespace Editor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ScriptEngine _engine;
        private FileToLog _output;

        private StepDirectory _stepDirectory;
        private CommandCenter _commandCenter;
        private ScriptScope _currentScope;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void InitializePythonEngine()
        {
            _engine = Python.CreateEngine(new Dictionary<string, object> {{"PrivateBinding", true}});
            
            _output = new FileToLog(_logger);
            _engine.GetSysModule().SetVariable("stdout", _output);
            _engine.GetSysModule().SetVariable("stderr", _output);
            // outra hipotese seria interceptar IO em _engine.Runtime.IO

            _engine.Runtime.LoadAssembly(GetType().Assembly);
            foreach (var referencedAssembly in GetType().Assembly.GetReferencedAssemblies())
            {
                _engine.Runtime.LoadAssembly(Assembly.Load(referencedAssembly));
            }
        }

        private object ExecuteCodeInCurrentScope(string pythonCode)
        {
            try
            {
                var script = _engine.CreateScriptSourceFromString(pythonCode);
                var code = script.Compile();
                return code.Execute(_currentScope);
            }
            catch (Exception ex)
            {
                WriteError(ex);
                return null;
            }
        }

        public class FileToLog
        {
            private readonly Logger _writeText;

            public FileToLog(Logger writeText)
            {
                _writeText = writeText;
            }

            // para substituir stderr e stdout em python apenas precisamos de 
            // implementar um metodo chamado write que recebe uma string
            public void write(string data)
            {
                _writeText.Info(data);
            }
        }

        #region Plumbing

        private Admin _adminWindow;
        private Log _logWindow;
        private Logger _logger;
        private IEnumerator _newStepEvaluationGuard;
        private CommandServices _commandServices;
        public Zoom Zoom{ get; set; }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            _logger = LogManager.GetLogger("Main");
            
            InitializeShortcuts();
            
            InitializePythonEngine();

            InitializePythonHighlighting();

            InitializeSteps();
            InitializeCommands();

            InitializeToolWindows();

            ConfigureLogging();

            Zoom = new Zoom(CodeGrid);
            _newStepEvaluationGuard = PrimeNewStep();
            _adminWindow.SelectFirst();
        }

        private void InitializeShortcuts()
        {
            GlobalShortcuts.InitializeShortcuts();
            CommandBindings.Add(new CommandBinding(GlobalShortcuts.Next, (o, e) => _adminWindow.NextStep()));
            CommandBindings.Add(new CommandBinding(GlobalShortcuts.Previous, (o, e) => _adminWindow.PreviousStep()));
            CommandBindings.Add(new CommandBinding(GlobalShortcuts.ZoomIn, (o, e) => Zoom.ZoomIn()));
            CommandBindings.Add(new CommandBinding(GlobalShortcuts.ZoomOut, (o, e) => Zoom.ZoomOut()));
            CommandBindings.Add(new CommandBinding(GlobalShortcuts.Execute, (o, e) => Execute_Click(this, e)));
            CommandBindings.Add(new CommandBinding(GlobalShortcuts.ToggleBottom, (o, e) => _commandCenter.ExecuteFromName("toggle", string.Empty)));
        }

        private void InitializeCommands()
        {
            _commandServices = new CommandServices(this, _stepDirectory);
            _commandCenter = new CommandCenter(FindPathForDirectory("commands"), _commandServices, _engine);
        }


        private void InitializeSteps()
        {
            _stepDirectory = new StepDirectory(FindPathForDirectory("steps"), _engine);
        }

        private void InitializeToolWindows()
        {
            _adminWindow = new Admin(_stepDirectory, _commandCenter)
                               {Owner = this};
            _adminWindow.Closing += (obj, evt) =>
                                        {
                                            evt.Cancel = true;
                                            if (_logWindow.IsActive)
                                                _adminWindow.Toggle();
                                        };

            _adminWindow.StepChanged += (sender, step) =>
                                            {
                                                step.Update(this);
                                                _newStepEvaluationGuard = PrimeNewStep();
                                            };

            _adminWindow.ExecuteCommandWithParameters += (command, parameters) =>
                                                             {
                                                                 _commandCenter.ExecuteFromName(command, parameters);
                                                             };
            _commandServices.Admin = _adminWindow;

            _logWindow = new Log() {Owner = this};
            _logWindow.Closing += (obj, evt) =>
                                      {
                                          evt.Cancel = true;
                                          if(_logWindow.IsActive)
                                            _logWindow.Toggle();
                                      };
        }

        private void InitializePythonHighlighting()
        {
            IHighlightingDefinition customHighlighting;
            using (Stream s = this.GetType().Assembly.GetManifestResourceStream("Editor.etc.Python.xshd"))
            {
                if (s == null)
                    throw new InvalidOperationException("Could not find embedded resource");
                using (XmlReader reader = new XmlTextReader(s))
                {
                    customHighlighting = ICSharpCode.AvalonEdit.Highlighting.Xshd.HighlightingLoader.Load(reader, HighlightingManager.Instance);
                }
            }
            HighlightingManager.Instance.RegisterHighlighting("Python", new[] { ".py" }, customHighlighting);

            TextEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinitionByExtension(".py");

        }

        private void ConfigureLogging()
        {
            var config = new LoggingConfiguration();
            config.AddTarget("debugWindow", _logWindow.Target);
            config.AddTarget("infoWindow", LogControl.Target);

            config.LoggingRules.Add(new LoggingRule("*", LogLevel.Debug, _logWindow.Target));
            config.LoggingRules.Add(new LoggingRule("*", LogLevel.Info, LogControl.Target));

            LogManager.Configuration = config;
        }

        private void WriteError(Exception exception)
        {
            _logger.Error(string.Format("Error:{0}\n", exception.Message));
            _logger.Debug(string.Format("{0}\n", exception.StackTrace));
        }

        private void Execute_Click(object sender, RoutedEventArgs e)
        {
            _newStepEvaluationGuard = PrimeNewStep();
            _newStepEvaluationGuard.MoveNext();
        }

        private void Interpreter_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Interpreter.Text = string.Empty;
                return;
            }
            if (e.Key == Key.Return)
            {
                _newStepEvaluationGuard.MoveNext();

                _logger.Info(string.Format("> {0}\n", Interpreter.Text));
                var returnValue = ExecuteCodeInCurrentScope(Interpreter.Text);
                if (returnValue != null)
                {
                    _logger.Info(string.Format("{0}\n", _engine.Operations.Format(returnValue)));
                }
            }
        }

        private void ToggleAdmin_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            _adminWindow.Left = this.Left + this.Width + 1;
            _adminWindow.Top = this.Top;
            _adminWindow.Toggle();
        }

        private void ToggleLog_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            _logWindow.Left = this.Left - _adminWindow.Width - 1;
            _logWindow.Top = this.Top;
            _logWindow.Toggle();
        }

        private IEnumerator PrimeNewStep()
        {
            _currentScope = _engine.CreateScope();
            // add a reference to this window to poke around in the internals
            _currentScope.SetVariable("this", this);

            ExecuteCodeInCurrentScope(TextEditor.Text);
            while (true)
            {
                yield return new object();
            }
        }

        private void TextEditor_KeyUp(object sender, KeyEventArgs e)
        {
            // ensure we exec when we have new text
            _newStepEvaluationGuard = PrimeNewStep();
        }

        private string FindPathForDirectory(string directoryName)
        {
            var path = Path.GetDirectoryName(this.GetType().Assembly.Location);
            while (Path.GetPathRoot(path) != path)
            {
                var directoryPath = Path.Combine(path, directoryName);
                if (Directory.Exists(directoryPath)) return directoryPath;

                path = Directory.GetParent(path).FullName;
            }
            throw new ApplicationException(string.Format("Directory {0} not found.", directoryName));
        }
        
        #endregion Plumbing
    }
}
