using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LegendPlugin
{
    public static class PersonMemory
    {
        private static string BaseDir =>
            Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        private static string FileProjektant => Path.Combine(BaseDir, "projektanci.txt");
        private static string FileSprawdzajacy => Path.Combine(BaseDir, "sprawdzajacy.txt");
        private static string FileOpracowujacy => Path.Combine(BaseDir, "opracowujacy.txt");
        private static string FileJednostka => Path.Combine(BaseDir, "jednostki.txt");
        private static string FileInwestor => Path.Combine(BaseDir, "inwestorzy.txt");
        private static string FileObiekt => Path.Combine(BaseDir, "obiekty.txt");
        private static string FileTytul => Path.Combine(BaseDir, "tytuly.txt");
        private static string FileSkala => Path.Combine(BaseDir, "skale.txt");

        public static List<string> LoadProjektanci() => Load(FileProjektant);
        public static List<string> LoadSprawdzajacy() => Load(FileSprawdzajacy);
        public static List<string> LoadOpracowujacy() => Load(FileOpracowujacy);
        public static List<string> LoadJednostki() => Load(FileJednostka);
        public static List<string> LoadInwestorzy() => Load(FileInwestor);
        public static List<string> LoadObiekty() => Load(FileObiekt);
        public static List<string> LoadTytuly() => Load(FileTytul);
        public static List<string> LoadSkale() => Load(FileSkala);

        public static void SaveProjektant(string val) => Save(FileProjektant, val);
        public static void SaveSprawdzajacy(string val) => Save(FileSprawdzajacy, val);
        public static void SaveOpracowujacy(string val) => Save(FileOpracowujacy, val);
        public static void SaveJednostka(string val) => Save(FileJednostka, val);
        public static void SaveInwestor(string val) => Save(FileInwestor, val);
        public static void SaveObiekt(string val) => Save(FileObiekt, val);
        public static void SaveTytul(string val) => Save(FileTytul, val);
        public static void SaveSkala(string val) => Save(FileSkala, val);

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
            if (!list.Contains(value.Trim()))
            {
                list.Add(value.Trim());
                File.WriteAllLines(path, list.OrderBy(x => x));
            }
        }
    }
}