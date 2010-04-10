using System;
using System.IO;
using System.Timers;

namespace Editor.Model
{
    internal class TimedDirectoryWatcher
    {
        private readonly Timer _timer;

        public TimedDirectoryWatcher(string directory , Action method)
        {
            var watcher = new FileSystemWatcher(directory);
            watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime |
                                   NotifyFilters.FileName;

            watcher.Changed += OnChange;
            watcher.Created += OnChange;
            watcher.Deleted += OnChange;
            watcher.Renamed += OnChange;
            watcher.EnableRaisingEvents = true;

            _timer = new Timer();
            _timer.Interval = 1000;
            _timer.Elapsed += (o, e) =>
                                  {
                                      _timer.Stop();
                                      method();
                                  };
                
        }

        private void OnChange(object sender, object e)
        {
            if(_timer.Enabled)
                _timer.Stop();
            _timer.Start();
        }
    }
}