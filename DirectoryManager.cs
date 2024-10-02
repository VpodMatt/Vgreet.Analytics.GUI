using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vgreet.Analytics.GUI
{
    public class DirectoryManager
    {
        private readonly List<DirectoryInfo> _directories;
        private readonly ProgressTask _dirTask;
        private readonly ProgressTask _fileTask;
        private readonly ProgressTask _processingTask;
        private readonly double _dirIncrement;
        private readonly double _fileIncrement;

        public DirectoryManager(List<DirectoryInfo> directories, ProgressTask directoryTask, ProgressTask fileTask, ProgressTask processingTask, double dirIncrement, double fileIncrement)
        {
            _directories = directories;
            _dirTask = directoryTask;
            _fileTask = fileTask;
            _processingTask = processingTask;
        }

        public async Task<YearlyAnalytics> Process()
        {
            List<FileInfo> files = [];

            foreach(var dir in _directories)
            {
                foreach(var file in dir.EnumerateFiles())
                {
                    files.Add(file);
                    _fileTask.Increment(_fileIncrement);
                }

                _dirTask.Increment(_dirIncrement);
            }

            var fileProcesser = new FileProcesser(files, _processingTask, _fileIncrement);
            return await fileProcesser.Process();
        }
    }
}
