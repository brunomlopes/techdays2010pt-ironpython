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

namespace Editor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ScriptEngine _engine;
        private MyFile _output;

        public MainWindow()
        {
            InitializeComponent();
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
            TextEditor.Text = @"print '''this is 
a string
with
several lines'''";
            _engine = Python.CreateEngine();
            _output = new MyFile(WriteText);
            _engine.GetSysModule().SetVariable("stdout", _output);
            _engine.GetSysModule().SetVariable("stderr", _output);
        }

       
        private void button1_Click(object sender, RoutedEventArgs e)
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

        private void WriteText(string str)
        {
            LogTextBlock.Text += str;
            LogScrollViewer.ScrollToBottom();
            System.Diagnostics.Debug.Write("text:" + str);
        }

        private void WriteError(string str)
        {
            LogTextBlock.Text += str;
            LogScrollViewer.ScrollToBottom();
            System.Diagnostics.Debug.Write("error:"+str);
        }

        public class MyFile
        {
            private readonly Action<string> _writeText;

            public MyFile(Action<string> writeText)
            {
                _writeText = writeText;
            }

            public void write(string data)
            {
                _writeText(data);
            }
        }

    }
}
