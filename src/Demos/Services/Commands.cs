using System;
using System.Collections.Generic;
using System.Linq;
using Demos.Commands;

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

            AvailableCommands = commandClasses.Select(cls => Activator.CreateInstance(cls) as ICommand);
        }

        public string ExecuteFromName(string name, string parameters)
        {
            var command = AvailableCommands.SingleOrDefault(c => c.Name.ToLowerInvariant() == name.ToLowerInvariant());

            if(command == null) return String.Empty;

            return command.Execute(parameters);
        }
    }
}