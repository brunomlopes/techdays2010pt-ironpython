using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Editor.Model
{
    public abstract class Step
    {
        public string Text { get; private set; }
        public string FilePath { get; private set; }
        public string FileName { get; private set; }
        public IStepMetadata Metadata { get; set; }
        protected abstract string Tab { get; }

        public Step(string filePath)
        {
            FilePath = filePath;
            FileName = Path.GetFileName(filePath);
            Text = File.ReadAllText(filePath);
        }

        public void Update(MainWindow window)
        {
            Metadata.Update(window, this);

            window.Tabs.SelectedItem = window.Tabs.FindName(Tab);
        }


        public override string ToString()
        {
            return Metadata.Name;
        }
    }

    [Handles(".png", ".jpg")]
    class ImageStep : Step
    {
        public ImageStep(string filePath) : base(filePath)
        {
            Metadata = new DefaultImageMetadata(filePath);
        }

        protected override string Tab
        {
            get { return "ImageTab"; }
        }
    }

    [Handles(".py")]
    class PythonStep : Step
    {
        public PythonStep(string filePath) : base(filePath)
        {
            Metadata = new DefaultPythonMetadata(filePath);
        }

        protected override string Tab
        {
            get { return "CodeTab"; }
        }
    }

    public interface IStepMetadata
    {
        string Name { get; }
        void Update(MainWindow window, Step step);
    }

    public class DefaultPythonMetadata : IStepMetadata
    {
        public string Name { get; private set; }

        public DefaultPythonMetadata(string stepFilePath)
        {
            Name = Path.GetFileNameWithoutExtension(stepFilePath);
        }

        public virtual void Update(MainWindow window, Step step)
        {
            window.TextEditor.Text = step.Text;
            window.TextEditor.Visibility = Visibility.Visible;

            window.Interpreter.Text = string.Empty;
            window.Interpreter.Visibility = Visibility.Visible;

            window.LogControl.Clear();
            window.LogControl.Visibility = Visibility.Visible;

            window.Execute.Visibility = Visibility.Visible;
        }
    }
    
    public class DefaultImageMetadata : IStepMetadata
    {
        public string Name { get; private set; }

        public DefaultImageMetadata(string stepFilePath)
        {
            Name = Path.GetFileNameWithoutExtension(stepFilePath);
        }

        public virtual void Update(MainWindow window, Step step)
        {
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.UriSource = new Uri(step.FilePath);
            bitmapImage.EndInit();

            window.Image.Source = bitmapImage;
        }
    }

    
    internal class HandlesAttribute : Attribute
    {
        public string[] Extensions { get; protected set; }

        public HandlesAttribute(params string[] extensions)
        {
            Extensions = extensions;
        }
    }
}