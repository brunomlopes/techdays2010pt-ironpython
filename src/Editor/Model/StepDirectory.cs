using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Editor.Extensions;
using IronPython.Runtime;
using Microsoft.Scripting.Hosting;
using NLog;

namespace Editor.Model
{
    public class StepDirectory
    {
        private readonly string _path;
        private readonly ScriptEngine _pythonEngine;
        private readonly Dictionary<string, Type> _typesByExtension;
        private IEnumerable<string> _extensions;
        private Logger _logger;
        private TimedDirectoryWatcher _watcher;

        public IEnumerable<Step> Steps { get; private set; }
        public event Action<StepDirectory, IEnumerable<Step>> StepsUpdated;

        public string Path
        {
            get { return _path; }
        }


        public StepDirectory(string path, ScriptEngine pythonEngine)
        {
            _path = path;
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
            var orderingFilePath = System.IO.Path.Combine(Path, "ordering");
            if(File.Exists(orderingFilePath))
            {
                order = File.ReadAllLines(orderingFilePath)
                    .Where(l => l.Trim() != String.Empty)
                    .Select((filename, i) => new {Filename = filename, i})
                    .ToDictionary(t => t.Filename, t => t.i);
            }
            
            var ignore = new List<string>();
            var ignoreFilePath = System.IO.Path.Combine(Path, "ignore");
            if (File.Exists(ignoreFilePath))
            {
                ignore = File.ReadAllLines(ignoreFilePath)
                    .Select(filename => filename.Trim())
                    .ToList();
            }

            Steps = Directory.GetFiles(Path)
                .Where(path => _extensions.Contains(System.IO.Path.GetExtension(path).ToLowerInvariant())
                               && !(System.IO.Path.GetFileName(path).EndsWith("_metadata.py"))
                               && !(ignore.Contains(System.IO.Path.GetFileName(path))))
                .Select(path => new { Path = path, Extension = System.IO.Path.GetExtension(path).ToLowerInvariant() })
                .Select(stepLocation => new { Type = _typesByExtension[stepLocation.Extension], stepLocation.Path })
                .Select(step => Activator.CreateInstance(step.Type, step.Path))
                .Cast<Step>()
                .OrderBy(s => order.ContainsKey(s.FileName) ? order[s.FileName] : int.MaxValue)
                .ToList();
        }

        private void LoadMetadataForSteps()
        {
            var metadataFiles = Directory.GetFiles(Path)
                .Where(f => System.IO.Path.GetFileName(f).EndsWith("_metadata.py"))
                .Select(
                    s => new
                             {
                                 StepFileName = System.IO.Path.GetFileName(s).Replace("_metadata.py", ""),
                                 MetadataFilePath = s,
                                 MetadataFileName = System.IO.Path.GetFileName(s)
                             });

            foreach (var metadataFile in metadataFiles)
            {
                try
                {
                    var step = Steps.SingleOrDefault(s => System.IO.Path.GetFileNameWithoutExtension(s.FileName) == metadataFile.StepFileName);
                    if(step == null)
                    {
                        _logger.Warn(string.Format("No step related to metadata {0}",
                                                   metadataFile.MetadataFileName));
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
                        _logger.Warn(string.Format("No IStepMetadata types found in {0}\n", metadataFile.MetadataFileName));
                        continue;
                    }
                    if(pythonStepMetadata.Count() > 1)
                    {
                        _logger.Warn(String.Format("Too many ({0}) IStepMetadata types found in {1}\n", pythonStepMetadata.Count(),
                                     metadataFile.MetadataFileName));
                        continue;
                    }
                    _logger.Debug(string.Format("Loaded metadata {0} for step {1}\n", metadataFile.MetadataFileName, step.FileName));
                    step.Metadata = pythonStepMetadata.Single();
                }
                catch (Exception e)
                {
                    _logger.Error(string.Format("Error fetching IStepMetadata from {0} : {1}\n", metadataFile.MetadataFileName, e.Message));
                    _logger.Debug(e.StackTrace);
                }
            }
        }

        private void InitializeFileWatcher()
        {
            _watcher = new TimedDirectoryWatcher(Path, () =>
                                                                 {
                                                                     _logger.Debug("Reloading steps\n");
                                                                     LoadStepsAndMetadata();
                                                                     StepsUpdated(this, Steps);
                                                                 });
        }
    }
}