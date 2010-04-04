using System;
using System.Windows;

namespace Demos.Commands
{
    public interface ICommand
    {
        string Name { get; }
        string Execute(string parameters);
    }

    public class MessageBoxCommand : ICommand
    {
        public string Name
        {
            get { return "MessageBox"; }
        }

        public string Execute(string parameters)
        {
            return MessageBox.Show(parameters).ToString();
        }
    }

    public class Echo : ICommand
    {
        public string Name
        {
            get { return "Echo"; }
        }

        public string Execute(string parameters)
        {
            return parameters;
        }
    }
}