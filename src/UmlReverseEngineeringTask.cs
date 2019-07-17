using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Paillave.SharpReverse;

namespace src
{
    public class UmlReverseEngineeringTask : Task
    {
        [Required]
        public string AssemblyPath { get; set; }
        public override bool Execute()
        {
            try
            {
                var reverseModel = new ReverseModel(this.AssemblyPath, false, null);
                var outputPath = System.IO.Path.ChangeExtension(this.AssemblyPath, "yuml");
                using (var sw = new System.IO.StreamWriter(outputPath))
                    reverseModel.WriteYuml(sw);
            }
            catch (System.Exception)
            {
            }
            return true;
        }
    }
}