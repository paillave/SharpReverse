using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.CommandLineUtils;

namespace Paillave.SharpReverse
{
    class Program
    {
        static void Main(string[] args)
        {
            var commandLineApplication = new CommandLineApplication(false);
            commandLineApplication.Name = "sharp reverse";
            commandLineApplication.FullName = "Sharp Reverse";
            commandLineApplication.Description = "Reverse engineer an assembly containing entities into a yuml file to get an UML diagram out of it";
            commandLineApplication.ShowInHelpText = true;
            var inputAssemblyPathOption = commandLineApplication.Option("-i | --input-assembly-path <inputAssemblyPath>", "Assembly path to inspect", CommandOptionType.SingleValue);
            var outputPathOption = commandLineApplication.Option("-o | --output-path <outputPath>", "Path of the output yuml file", CommandOptionType.SingleValue);
            var rootClassNameOption = commandLineApplication.Option("-r | --root-class-name <rootClassName>", "Keep only classes that are assignable to this type", CommandOptionType.SingleValue);
            var noOrphansOption = commandLineApplication.Option("-no | --no-orphans", "Exclude entities that are not linked to any other entity", CommandOptionType.NoValue);
            commandLineApplication.HelpOption("-? | -h | --help");
            commandLineApplication.OnExecute(() =>
            {
                if (!inputAssemblyPathOption.HasValue())
                    commandLineApplication.ShowHelp();
                else
                    ExecuteApp(
                        args,
                        inputAssemblyPathOption.Value(),
                        outputPathOption.HasValue() ? outputPathOption.Value() : (string)null,
                        rootClassNameOption.HasValue() ? rootClassNameOption.Value() : (string)null,
                        noOrphansOption.HasValue()
                    );
                return 0;
            });
            commandLineApplication.Execute(args);
        }
        private static void ExecuteApp(string[] args, string inputAssemblyPath, string outputPath, string rootClassName, bool noOrphans)
        {
            // var inputAssemblyPath = args[0];
            var reverseModel = new ReverseModel(inputAssemblyPath, rootClassName, noOrphans);
            outputPath = outputPath ?? Path.ChangeExtension(inputAssemblyPath, "yuml");
            using (var sw = new StreamWriter(outputPath))
                reverseModel.WriteYuml(sw);
            Console.WriteLine("model written");
        }
    }
}
