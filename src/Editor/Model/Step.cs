using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Editor;

namespace Editor.Model
{
    public class StepDirectory
    {
        public IEnumerable<Step> Steps { get; private set; }

        public StepDirectory(string directory)
        {
            var files = Directory.GetFiles(directory, "step_*.py")
                .Where(f => !f.StartsWith("step_metadata_"));
            Steps = files
                .OrderBy(filePath => filePath)
                .Select(filePath => new Step(filePath));
        }
    }

    public class Step
    {
        public string Text { get; private set; }
        public string FilePath { get; private set; }
        public DefaultMetadata Metadata { get; private set; }

        public Step(string filePath)
        {
            FilePath = filePath;
            Text = File.ReadAllText(filePath);
            Metadata = new DefaultMetadata(filePath);
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
            // NOOP
        }
    }
}