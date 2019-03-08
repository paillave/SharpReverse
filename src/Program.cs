using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Paillave.SharpReverse
{
    class Program
    {
        static void Main(string[] args)
        {
            var inputAssemblyPath = args[0];
            var reverseModel = new ReverseModel(inputAssemblyPath);
            var outputPath = args.Length > 1 ? args[1] : Path.ChangeExtension(inputAssemblyPath, "yuml");
            using (var sw = new StreamWriter(outputPath))
                reverseModel.WriteYuml(sw);
            Console.WriteLine("model writen");
        }
    }
}
