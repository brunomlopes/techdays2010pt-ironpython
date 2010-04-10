using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Editor;

namespace Editor.Model
{
    public abstract class Step
    {
        public string Text { get; private set; }
        public string FilePath { get; private set; }
        public IStepMetadata Metadata { get; private set; }

        public Step(string filePath)
        {
            FilePath = filePath;
            Text = File.ReadAllText(filePath);
            Metadata = new DefaultMetadata(filePath);
        }

        public abstract void Update(MainWindow window);

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
        }

        public override void Update(MainWindow window)
        {
            window.TextEditor.Text = "Should display photo from " + FilePath;
            window.Tabs.SelectedItem = window.Tabs.FindName("ImageTab");
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.UriSource = new Uri(FilePath);
            bitmapImage.EndInit();
            window.Image.Source = bitmapImage;
        }
    }

    [Handles(".py")]
    class PythonStep : Step
    {
        public PythonStep(string filePath) : base(filePath)
        {
        }

        public override void Update(MainWindow window)
        {
            Metadata.Update(window, this);
            window.TextEditor.Text = Text;
            window.Interpreter.Text = string.Empty;
            window.LogControl.Clear();
            window.Tabs.SelectedItem = window.Tabs.FindName("CodeTab");
        }
    }

    public interface IStepMetadata
    {
        string Name { get; }
        void Update(MainWindow window, Step step);
    }

    public class DefaultMetadata : IStepMetadata
    {
        public string Name { get; private set; }

        public DefaultMetadata(string stepFilePath)
        {
            Name = Path.GetFileNameWithoutExtension(stepFilePath);
        }

        public virtual void Update(MainWindow window, Step step)
        {
            // NO-OP
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