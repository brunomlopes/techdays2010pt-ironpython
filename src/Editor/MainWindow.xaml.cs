using System;
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
using ICSharpCode.AvalonEdit.Highlighting;
using IronPython.Hosting;
using IronPython.Runtime;
using Microsoft.Scripting.Hosting;
using Microsoft.Scripting.Runtime;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Win32.Targets;

namespace Editor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ScriptEngine _engine;
        private FileToLog _output;
        private Admin _adminWindow;
        private Log _logWindow;
        private Logger _logger;

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
        }

        private void Execute_Click(object sender, RoutedEventArgs e)
        {
            _engine.Runtime.LoadAssembly(this.GetType().Assembly);

            try
            {
                ScriptSource script = _engine.CreateScriptSourceFromString(TextEditor.Text);
                CompiledCode code = script.Compile();
                ScriptScope scope = _engine.CreateScope();

                code.Execute(scope);
            }
            catch (Exception ex)
            {
                WriteError(string.Format("Error : {0}", ex));

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

       

        private void WriteError(string str)
        {
            _logger.Error(str);
            System.Diagnostics.Debug.Write("error:"+str);
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

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            GlobalShortcuts.InitializeShortcuts();

            InitializePythonHighlighting();

            TextEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinitionByExtension(".py");
            TextEditor.Text = @"print '''this is 
a string
with
several lines'''";

            _adminWindow = new Admin() {Owner = this, WindowStyle = WindowStyle.ToolWindow, ShowActivated = false};
            _adminWindow.Closing += (obj, evt) =>
                                        {
                                            evt.Cancel = true;
                                            _adminWindow.Toggle();
                                        };
            _logWindow = new Log() {Owner = this, WindowStyle = WindowStyle.ToolWindow, ShowActivated = false};
            _logWindow.Closing += (obj, evt) =>
                                      {
                                          evt.Cancel = true;
                                          _logWindow.Toggle();
                                      };

            InitializeLogging();

            InitializePythonEngine();
        }

        #endregion Plumbing

    }

    
}
