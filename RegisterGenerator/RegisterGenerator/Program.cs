using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegisterGenerator
{
    class Program
    {
        private static string IndexGeneratorName = "songidx.exe";
        private static string IndexFileName = "titleidx";
        
        private static IDictionary<string, string> IndexedLines = new Dictionary<string, string>();

        static void Main(string[] args)
        {
            var filename = args.Any() ? args[0] : IndexFileName;

            Console.WriteLine("Genererar indexfiler för " + filename);

            var p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = IndexGeneratorName;
            p.StartInfo.Arguments = filename + ".sxd " + filename + ".sbx";
            p.OutputDataReceived += (object sender, DataReceivedEventArgs e) => Console.WriteLine(e.Data);
            p.Start();
            p.BeginOutputReadLine();
            p.WaitForExit();

            Console.WriteLine("Indexfiler genererade, läser in resultatet");

            var indexLines = File.ReadAllLines(filename + ".sbx");
            
            Console.WriteLine("Ordnar raderna enligt svenskt alfabete");

            foreach (var line in indexLines)
            {
                if (line.StartsWith("\\idxentry") || line.StartsWith("\\idxaltentry"))
                {
                    var start = line.IndexOf('{') + 1;
                    var end = line.IndexOf('}');
                    var title = line.Substring(start, end - start);

                    if (!IndexedLines.ContainsKey(title))
                    {
                        IndexedLines.Add(title, line);
                    }
                    else
                    {
                        Console.WriteLine("VARNING: Hittade flera inlägg av sången " + title);
                    }
                }
            }

            var culture = new CultureInfo("sv-SE");
            var comparer = StringComparer.Create(culture, true);
            IndexedLines = IndexedLines
                .OrderBy(i => i.Key.First().ToString(), comparer)
                .ThenBy(i => i.Key.Substring(1), comparer)
                .ToDictionary(d => d.Key, d => d.Value);

            var charInOrder = 'A';
            var lines = new List<string>
            {
                "\\begin{idxblock}{" + charInOrder + "}"
            };

            foreach (var line in IndexedLines)
            {
                var firstChar = line.Key.First();
                if (comparer.Compare(firstChar.ToString(), charInOrder.ToString()) != 0)
                {
                    charInOrder = firstChar;

                    lines.Add("\\end{idxblock}");
                    lines.Add("\\begin{idxblock}{" + charInOrder + "}");
                }

                lines.Add(line.Value);
            }

            lines.Add("\\end{idxblock}");

            Console.WriteLine("Sångtitlarna ordnade, skriver till filen " + filename + ".sbx");

            File.WriteAllLines(filename + ".sbx", lines);

            Console.WriteLine("Skrivning klar, avsluta genom att trycka vilken tangent som helst");

            Console.ReadKey();
        }
    }
}
