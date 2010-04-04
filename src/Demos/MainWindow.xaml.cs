using System;
using System.Collections.Generic;
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

namespace Demos
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Services.Commands _commands;
        public MainWindow()
        {
            InitializeComponent();
            _commands = new Services.Commands();
            Input.ItemsSource = _commands.AvailableCommands.Select(c => c.Name);
            Input.KeyUp +=
                (obj, e) =>
                    {
                        if (e.Key == Key.Return) Execute(SanitizeText(Input.Text));
                    };
            Exec.Click += (obj, e) => Execute(SanitizeText(Input.Text));
                  
        }
        private string SanitizeText(string text)
        {
            return text.Trim(new[] {' ', '\n'});
        }
        private void Execute(string text)
        {
            var splitText = text.Split(' ');
            
            var name = splitText.First();
            var parameters = string.Join(" ", Input.Text.Split(' ').Skip(1).ToArray());

            var result = _commands.ExecuteFromName(name, parameters);
            Output.Text += string.Format(">{0}\n{1}\n", text, result);

            Input.Text = string.Empty;
        }
    }
}
