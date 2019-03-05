using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using System.Linq;
using ActiveXConnectLib;

namespace SingleThread
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("ECR implementation example v2.0");
                ReadCliArgs(args);
                new ECR().Run();
            }
            catch (Exception ex)
            {
                ConsoleGui.Error(ex.ToString());
                Console.WriteLine("Press enter to exit...");
                Console.ReadLine();
            }
        }

        static void ReadCliArgs(string[] args)
        {
            if (args== null || args.Length == 0) return;
            var argsLower = args.Select(a => a.ToLower()).ToArray();
            var i = Array.FindIndex(argsLower, s => s == "--working-dir");
            if (i >= 0 && args.Length > i) { DocumentManager.SetWorkingPath(args[i + 1]); }
        }
    }
}

