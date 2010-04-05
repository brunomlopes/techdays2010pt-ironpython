using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Demos.Commands;
using IronPython.Hosting;
using IronPython.Runtime;
using Microsoft.Scripting.Hosting;

namespace Demos.Services
{
    public class Commands
    {
        public IEnumerable<ICommand> AvailableCommands { get; private set; }

        public Commands()
        {
            var commandClasses = this.GetType().Assembly.GetTypes()
                .Where(cls => typeof (ICommand).IsAssignableFrom(cls))
                .Where(cls => !cls.IsInterface && !cls.IsAbstract);

            AvailableCommands = commandClasses
                .Select(cls => Activator.CreateInstance(cls) as ICommand)
                .Concat(GetPythonCommands());
        }

        public string ExecuteFromName(string name, string parameters)
        {
            var command = AvailableCommands.SingleOrDefault(c => c.Name.ToLowerInvariant() == name.ToLowerInvariant());

            if(command == null) return String.Empty;

            try
            {
                return command.Execute(parameters);
            }
            catch (Exception e)
            {
                return string.Format("Error executing command '{0}' : '{1}'", command.Name, e.Message);
            }
        }

        private IEnumerable<ICommand> GetPythonCommands()
        {
            var engine = Python.CreateEngine();
            engine.Runtime.LoadAssembly(this.GetType().Assembly);

            var commands = new List<ICommand>();

            var pythonFiles =
                Directory
                    .GetFiles("Commands")
                    .Where(f => f.ToLowerInvariant().EndsWith("ipy"));

            foreach (var pythonFile in pythonFiles)
            {
                try
                {
                    ScriptSource script = engine.CreateScriptSourceFromFile(pythonFile);
                    CompiledCode code = script.Compile();
                    ScriptScope scope = engine.CreateScope();
                    
                    code.Execute(scope);

                    var pythonCommands = scope.GetItems()
                        .Select(kvp => kvp.Value)
                        .OfType<ICommand>();

                    commands.AddRange(pythonCommands);
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.Write(string.Format("Error with file {1}: {0}", e, pythonFile));
                }
            }
            return commands;
        }
    }
}