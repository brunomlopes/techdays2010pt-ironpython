using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using Enumerable = System.Linq.Enumerable;

namespace Editor.Model
{
    public class StepDirectory
    {
        private readonly string _directory;
        private readonly Dictionary<string, Type> _typesByExtension;
        private IEnumerable<string> _extensions;
        private Logger _logger;
        private TimedDirectoryWatcher _watcher;

        public IEnumerable<Step> Steps { get; private set; }
        public event Action<StepDirectory, IEnumerable<Step>> StepsUpdated;

        public StepDirectory(string directory)
        {
            _directory = directory;
            _logger = LogManager.GetLogger("steps");
            var stepTypes = this.GetType().Assembly.GetTypes()
                .Where(t => !t.IsAbstract)
                .Where(t => t.IsSubclassOf(typeof (Step)))
                .Select(t => new
                                 {
                                     Type = t,
                                     Extensions = t.GetCustomAttributes(typeof (HandlesAttribute), true)
                                 .Cast<HandlesAttribute>()
                                 .SelectMany(h => h.Extensions)
                                 });
            _extensions = stepTypes.SelectMany(t => t.Extensions);
            _typesByExtension = _extensions.ToDictionary(ext => ext.ToLowerInvariant(),
                                                        ext => stepTypes.Single(s => s.Extensions.Contains(ext)).Type);


            var metadataFiles = Directory.GetFiles(_directory)
                .Where(f => !Path.GetFileName(f).EndsWith("_metadata.py"));

            LoadSteps();

            InitializeFileWatcher();
        }

        private void LoadSteps()
        {
            var order = new Dictionary<string, int>();
            var orderingFilePath = Path.Combine(_directory, "ordering");
            if(File.Exists(orderingFilePath))
            {
                order = File.ReadAllLines(orderingFilePath)
                    .Select((filename, i) => new {Filename = filename, i})
                    .ToDictionary(t => t.Filename, t => t.i);
            }
            Steps = Directory.GetFiles(_directory)
                .Where(path => _extensions.Contains(Path.GetExtension(path).ToLowerInvariant()))
                .Select(path => new {Path = path, Extension = Path.GetExtension(path).ToLowerInvariant()})
                .Select(stepLocation => new {Type = _typesByExtension[stepLocation.Extension], stepLocation.Path})
                .Select(step => Activator.CreateInstance(step.Type, step.Path))
                .Cast<Step>()
                .ToList() // Force the lazy basterd into a eager list :)
                .OrderBy(s => order.ContainsKey(s.FileName) ? order[s.FileName] : int.MaxValue); 
        }

        private void InitializeFileWatcher()
        {
            _watcher = new TimedDirectoryWatcher(_directory, () =>
                                                                 {
                                                                     _logger.Debug("Reloading steps");
                                                                     LoadSteps();
                                                                     StepsUpdated(this, Steps);
                                                                 });
        }
    }
}