using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using IronPython.Runtime;
using Microsoft.Scripting.Hosting;
using NLog;

namespace Editor.Model
{
    public class CommandCenter
    {
        public IEnumerable<ICommand> AvailableCommands
        {
            get { return _currentCommands.Values.SelectMany(k => k).Concat(_cSharpCommands); }
        }

        private readonly string _directory;
        private readonly CommandServices _commandServices;
        private readonly ScriptEngine _pythonEngine;
        private readonly Dictionary<string, List<ICommand>> _currentCommands;
        private readonly List<ICommand> _cSharpCommands;
        private readonly Logger _logger;
        private TimedDirectoryWatcher _watcher;

        public CommandCenter(string directory, CommandServices commandServices, ScriptEngine pythonEngine)
        {
            _directory = directory;
            _commandServices = commandServices;
            _pythonEngine = pythonEngine;
            _logger = LogManager.GetLogger("commands");
            _currentCommands = new Dictionary<string, List<ICommand>>();

             var commandClasses = this.GetType().Assembly.GetTypes()
                .Where(cls => typeof (ICommand).IsAssignableFrom(cls))
                .Where(cls => !cls.IsInterface && !cls.IsAbstract);

            _cSharpCommands = commandClasses
                .Select(cls => Activator.CreateInstance(cls) as ICommand)
                .ToList();

            LoadPythonCommands();
            InitializeFileWatcher();
        }

        private void LoadPythonCommands()
        {
            var pythonFiles =
                Directory
                    .GetFiles(_directory)
                    .Where(f => f.ToLowerInvariant().EndsWith("ipy"));

            foreach (var pythonFile in pythonFiles)
            {
                try
                {
                    ScriptSource script = _pythonEngine.CreateScriptSourceFromFile(pythonFile);
                    CompiledCode code = script.Compile();
                    ScriptScope scope = _pythonEngine.CreateScope();

                    scope.SetVariable("ICommand",  ClrModule.GetPythonType(typeof(ICommand)));
                    scope.SetVariable("services",  _commandServices);
                    code.Execute(scope);

                    var pythonCommandTypes = scope.GetItems()
                        .Select(kvp => kvp.Value)
                        .OfType<IronPython.Runtime.Types.PythonType>()
                        .Where(pythonType => typeof(ICommand).IsAssignableFrom(pythonType.__clrtype__()))
                        .Where(pythonType => !pythonType.__clrtype__().IsAbstract);

                    var pythonCommands = pythonCommandTypes
                        .Select(pythonType => Activator.CreateInstance(pythonType.__clrtype__(), pythonType))
                        .OfType<ICommand>()
                        .ToList();

                    _currentCommands[pythonFile] = pythonCommands;
                }
                catch (Exception e)
                {
                    _logger.Error(string.Format("Error with file {0}: {1}", pythonFile, e.Message));
                    _logger.Debug(e.StackTrace);
                }
            }
        }

        public void ExecuteFromName(string name, string parameters)
        {
            var command = AvailableCommands.SingleOrDefault(c => c.Name.ToLowerInvariant() == name.ToLowerInvariant());

            if(command == null) return ;

            try
            {
                command.Execute(parameters);
            }
            catch (Exception e)
            {
                _logger.Error(string.Format("Error executing command '{0}' : '{1}'", command.Name, e.Message));
                _logger.Debug(e.StackTrace);
            }
        }

        private void InitializeFileWatcher()
        {
            _watcher = new TimedDirectoryWatcher(_directory, () =>
                                                                 {
                                                                     _logger.Debug("Reloading python commands");
                                                                     LoadPythonCommands();
                                                                 });
        }
    }
}