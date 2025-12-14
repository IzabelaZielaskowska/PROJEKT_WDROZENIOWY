using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace LegendPlugin
{
    public static class PersonMemory
    {
        private static string BaseDir =>
            Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        private static string FileProjektant => Path.Combine(BaseDir, "projektanci.txt");
        private static string FileSprawdzajacy => Path.Combine(BaseDir, "sprawdzajacy.txt");
        private static string FileOpracowujacy => Path.Combine(BaseDir, "opracowujacy.txt");

        public static List<string> LoadProjektanci() => Load(FileProjektant);
        public static List<string> LoadSprawdzajacy() => Load(FileSprawdzajacy);
        public static List<string> LoadOpracowujacy() => Load(FileOpracowujacy);

        public static void SaveProjektant(string val) => Save(FileProjektant, val);
        public static void SaveSprawdzajacy(string val) => Save(FileSprawdzajacy, val);
        public static void SaveOpracowujacy(string val) => Save(FileOpracowujacy, val);

        private static List<string> Load(string path)
        {
            if (!File.Exists(path)) return new List<string>();
            return File.ReadAllLines(path)
                       .Where(l => !string.IsNullOrWhiteSpace(l))
                       .Distinct()
                       .OrderBy(l => l)
                       .ToList();
        }

        private static void Save(string path, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;

            var list = Load(path);
            if (!list.Contains(value))
            {
                list.Add(value);
                File.WriteAllLines(path, list.OrderBy(x => x));
            }
        }
    }
}