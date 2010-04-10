using System.Windows;

namespace Editor.Model
{
    public interface ICommand
    {
        string Name { get; }
        void Execute(string parameters);
    }
}