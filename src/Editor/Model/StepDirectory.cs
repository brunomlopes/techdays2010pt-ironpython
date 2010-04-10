using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Enumerable = System.Linq.Enumerable;

namespace Editor.Model
{
    public class StepDirectory
    {
        public IEnumerable<Step> Steps { get; private set; }

        public StepDirectory(string directory)
        {
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
            var extensions = stepTypes.SelectMany(t => t.Extensions);
            var typesByExtension = extensions.ToDictionary(ext => ext.ToLowerInvariant(),
                                                           ext => stepTypes.Single(s => s.Extensions.Contains(ext)).Type);


            var metadataFiles = Directory.GetFiles(directory)
                .Where(f => !Path.GetFileName(f).EndsWith("_metadata.py"));

            Steps = Directory.GetFiles(directory)
                .Where(path => extensions.Contains(Path.GetExtension(path).ToLowerInvariant()))
                .Select(path => new {Path = path, Extension = Path.GetExtension(path).ToLowerInvariant()})
                .Select(stepLocation => new {Type = typesByExtension[stepLocation.Extension], stepLocation.Path})
                .Select(step => Activator.CreateInstance(step.Type, step.Path))
                .Cast<Step>();
        }
    }
}