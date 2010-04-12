using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Editor.Extensions;
using IronPython.Runtime;
using IronPython.Runtime.Types;
using Microsoft.Scripting.Hosting;
using NLog;
using Enumerable = System.Linq.Enumerable;

namespace Editor.Model
{
    public class StepDirectory
    {
        private readonly string _directory;
        private readonly ScriptEngine _pythonEngine;
        private readonly Dictionary<string, Type> _typesByExtension;
        private IEnumerable<string> _extensions;
        private Logger _logger;
        private TimedDirectoryWatcher _watcher;

        public IEnumerable<Step> Steps { get; private set; }
        public event Action<StepDirectory, IEnumerable<Step>> StepsUpdated;

        public StepDirectory(string directory, ScriptEngine pythonEngine)
        {
            _directory = directory;
            _pythonEngine = pythonEngine;
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

            LoadStepsAndMetadata();

            InitializeFileWatcher();
        }

        private void LoadStepsAndMetadata()
        {
            LoadSteps();

            LoadMetadataForSteps();
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
            
            var ignore = new List<string>();
            var ignoreFilePath = Path.Combine(_directory, "ignore");
            if (File.Exists(ignoreFilePath))
            {
                ignore = File.ReadAllLines(ignoreFilePath)
                    .Select(filename => filename.Trim())
                    .ToList();
            }

            Steps = Directory.GetFiles(_directory)
                .Where(path => _extensions.Contains(Path.GetExtension(path).ToLowerInvariant())
                               && !(Path.GetFileName(path).EndsWith("_metadata.py"))
                               && !(ignore.Contains(Path.GetFileName(path))))
                .Select(path => new { Path = path, Extension = Path.GetExtension(path).ToLowerInvariant() })
                .Select(stepLocation => new { Type = _typesByExtension[stepLocation.Extension], stepLocation.Path })
                .Select(step => Activator.CreateInstance(step.Type, step.Path))
                .Cast<Step>()
                .OrderBy(s => order.ContainsKey(s.FileName) ? order[s.FileName] : int.MaxValue)
                .ToList();
        }

        private void LoadMetadataForSteps()
        {
            var metadataFiles = Directory.GetFiles(_directory)
                .Where(f => Path.GetFileName(f).EndsWith("_metadata.py"))
                .Select(
                    s => new
                             {
                                 StepFileName = Path.GetFileName(s).Replace("_metadata.py", ""),
                                 MetadataFilePath = s,
                                 MetadataFileName = Path.GetFileName(s)
                             });

            foreach (var metadataFile in metadataFiles)
            {
                try
                {
                    var step = Steps.SingleOrDefault(s => Path.GetFileNameWithoutExtension(s.FileName) == metadataFile.StepFileName);
                    if(step == null)
                    {
                        _logger.Warn("No step related to metadata {0}",
                                     metadataFile.MetadataFileName);
                        continue;
                    }
                    ScriptSource script = _pythonEngine.CreateScriptSourceFromFile(metadataFile.MetadataFilePath);
                    CompiledCode code = script.Compile();
                    ScriptScope scope = _pythonEngine.CreateScope();

                    scope.SetVariable("IStepMetadata", ClrModule.GetPythonType(typeof(IStepMetadata)));
                    scope.SetVariable("DefaultPythonMetadata", ClrModule.GetPythonType(typeof(DefaultPythonMetadata)));
                    
                    code.Execute(scope);
                    
                    var pythonStepMetadataTypes = scope.GetImplementationsOf<IStepMetadata>()
                        .Where(pythonType => pythonType.__clrtype__() != typeof(DefaultPythonMetadata));
                    
                    var pythonStepMetadata = pythonStepMetadataTypes
                        .Select(pythonType => _pythonEngine.Operations.CreateInstance(pythonType, step.FilePath))
                        .Cast<IStepMetadata>();
                    
                    if(pythonStepMetadata.Count() == 0)
                    {
                        _logger.Warn("No IStepMetadata types found in {0}", metadataFile.MetadataFileName);
                        continue;
                    }
                    if(pythonStepMetadata.Count() > 1)
                    {
                        _logger.Warn("Too many ({0}) IStepMetadata types found in {1}", pythonStepMetadata.Count(),
                                     metadataFile.MetadataFileName);
                        continue;
                    }
                    step.Metadata = pythonStepMetadata.Single();
                }
                catch (Exception e)
                {
                    _logger.Error("Error fetching IStepMetadata from {0} : {1}", metadataFile.MetadataFileName, e.Message);
                    _logger.Debug(e.StackTrace);
                }
            }
        }

        private void InitializeFileWatcher()
        {
            _watcher = new TimedDirectoryWatcher(_directory, () =>
                                                                 {
                                                                     _logger.Debug("Reloading steps");
                                                                     LoadStepsAndMetadata();
                                                                     StepsUpdated(this, Steps);
                                                                 });
        }
    }
}