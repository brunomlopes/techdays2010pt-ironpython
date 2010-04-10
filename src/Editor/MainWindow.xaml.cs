using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using Editor.Model;
using ICSharpCode.AvalonEdit.Highlighting;
using IronPython.Hosting;
using IronPython.Runtime;
using Microsoft.Scripting.Hosting;
using Microsoft.Scripting.Runtime;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Win32.Targets;
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
        private ScriptScope _currentScope;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void InitializePythonEngine()
        {
            _engine = Python.CreateEngine();
            _output = new FileToLog(_logger);
            _engine.GetSysModule().SetVariable("stdout", _output);
            _engine.GetSysModule().SetVariable("stderr", _output);
            _engine.Runtime.LoadAssembly(this.GetType().Assembly);
        }

        private void Execute_Click(object sender, RoutedEventArgs e)
        {
            _newStepEvaluationGuard = PrimeNewStep();
            _newStepEvaluationGuard.MoveNext();
        }

        private void Interpreter_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Return) return;
            if (e.Key == Key.Escape)
            {
                Interpreter.Text = string.Empty;
                return;
            }

            _newStepEvaluationGuard.MoveNext();

            _logger.Info(string.Format("> {0}\n", Interpreter.Text));
            var returnValue = ExecuteCodeInCurrentScope(Interpreter.Text);
            if(returnValue != null)
            {
                _logger.Info(string.Format("{0}\n", returnValue));
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

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            GlobalShortcuts.InitializeShortcuts();

            InitializePythonHighlighting();

            InitializeSteps();

            InitializeToolWindows();

            InitializeLogging();

            InitializePythonEngine();

            _newStepEvaluationGuard = PrimeNewStep();
        }

        private void InitializeSteps()
        {
            _stepDirectory = new StepDirectory(Path.Combine(Path.GetDirectoryName(this.GetType().Assembly.Location), "steps"));
        }

        private void InitializeToolWindows()
        {
            _adminWindow = new Admin(_stepDirectory) {Owner = this, WindowStyle = WindowStyle.ToolWindow, ShowActivated = false};
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
            _logWindow = new Log() {Owner = this, WindowStyle = WindowStyle.ToolWindow, ShowActivated = false};
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

        private void InitializeLogging()
        {
            var config = new LoggingConfiguration();
            config.AddTarget("debugWindow", _logWindow.Target);
            config.AddTarget("infoWindow", LogControl.Target);

            config.LoggingRules.Add(new LoggingRule("*", LogLevel.Debug, _logWindow.Target));
            config.LoggingRules.Add(new LoggingRule("*", LogLevel.Info, LogControl.Target));

            LogManager.Configuration = config;
            _logger = LogManager.GetLogger("Main");
        }


        private void WriteError(Exception exception)
        {
            _logger.Error(string.Format("Error:{0}\n", exception.Message));
            _logger.Debug(string.Format("{0}\n", exception.StackTrace));
            System.Diagnostics.Debug.Write("error:"+exception);
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

            ExecuteCodeInCurrentScope(TextEditor.Text);
            while (true)
            {
                yield return new object();
            }
        }
        
        #endregion Plumbing

        

    }

    
}
