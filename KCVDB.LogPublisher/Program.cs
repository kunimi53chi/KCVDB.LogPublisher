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
            if (Parser.Default.ParseArgumentsStrict(args, options))
            {
                var stateInputPath = options.StateInputPath;
                var inputPath = options.InputPath;
                var stateOutputPath = options.StateOutputPath;
                var outputPath = options.OutputPath;
                var publisher = new Publisher(stateInputPath, stateOutputPath, outputPath);
                var logFiles = new LogDirectory(inputPath).Publish();
                logFiles
                    .Subscribe(logFile =>
                    {
                        logFile.Subscribe(line =>
                        {
                            publisher.Add(line);
                        });
                    },
                    () =>
                    {
                        publisher.Dispose();
                    });
                logFiles
                    .Where(logFile => options.Delete)
                    .Subscribe(logFile =>
                    {
                        File.Delete(logFile.Path);
                    });
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

            [Option("state-input")]
            public string StateInputPath { get; set; }

            [Option("input", Required = true)]
            public string InputPath { get; set; }

            [Option("state-output", Required = true)]
            public string StateOutputPath { get; set; }

            [Option("output", Required = true)]
            public string OutputPath { get; set; }
        }
    }
}
