using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using CommandLine;
using KCVDB.LocalAnalyze;

namespace KCVDB.LogFilePublisher
{
    static class Program
    {
        private static void Main(string[] args)
        {
            var options = new Options();
            if (Parser.Default.ParseArgumentsStrict(args, options) && options.Paths.Count == 3)
            {
                var statePath = options.Paths[0];
                var inputPath = options.Paths[1];
                var outputPath = options.Paths[2];
                var publisher = new Publisher(statePath, outputPath);
                var logFiles = new LogDirectory(inputPath).Publish();
                logFiles
                    .Subscribe(logFile =>
                    {
                        logFile.Subscribe(line =>
                        {
                            publisher.Add(line);
                        });
                    });
                logFiles
                    .Where(logFile => options.Delete)
                    .Subscribe(logFile =>
                    {
                        File.Delete(logFile.Path);
                    });
                logFiles
                    .Finally(() =>
                    {
                        publisher.Dispose();
                    })
                    .Subscribe();
                logFiles.Connect();
            }
            else
            {
                throw new ArgumentException();
            }
        }

        private sealed class Options
        {
            [Option("delete")]
            public bool Delete { get; set; }

            [ValueList(typeof(List<string>))]
            public List<string> Paths { get; set; }
        }
    }
}
