using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SingleThread
{
    class DocumentManager
    {
        static string WorkingDir = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);
        public static void SetWorkingPath(string Path)
        {
            if(!System.IO.Directory.Exists(Path))
            {
                throw new Exception($"Direcotry not exist [{Path}]");
            }
            WorkingDir = Path;
            ConsoleGui.Info($"Using dir [{WorkingDir}]");
        }

        public static List<Document> GetAllDocuments()
        {
            List<Document> result = new List<Document>();

            foreach (string f in Directory.GetFiles(WorkingDir, "*.xml", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    if (f.EndsWith(".xml"))
                    {
                        Document d = Document.FromXml(File.ReadAllText(f));
                        result.Add(d);
                    }
                }
                catch { } // Ignore document that cannot be deserialized
            }

            return result;
        }

        public static void SaveDocumentToFile(Document doc)
        {
            var path = Path.Combine(WorkingDir, doc.DocumentNr + ".xml");
            File.WriteAllText(path, doc.ToXml());
        }

        public static void DeleteDocument(Document doc)
        {
            string filename = Path.Combine(WorkingDir, doc.DocumentNr + ".xml");
            if (File.Exists(filename))
                File.Delete(filename);
        }
    }
}
