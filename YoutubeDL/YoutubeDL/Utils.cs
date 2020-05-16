using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace YoutubeDL
{
    class Utils
    {
        public async static Task<List<string>> LoadUrlsAsync(string path)
        {
            var results = new List<string>();
            var reg = new Regex(@"http(s)?://([\w-]+\.)+[\w-]+(/[\w- ./?%&=]*)?");
            using(var reader = new StreamReader(path, Encoding.UTF8))
            {
                string line;
                while((line = await reader.ReadLineAsync()) != null)
                {
                    var match = reg.Match(line);
                    if (match.Success && !results.Contains(line))
                    {
                        results.Add(line);
                    }
                }
            }

            return results;
        }

        public static void Logging(string path, Dictionary<string, bool> results)
        {
            using (var writer = new StreamWriter(path))
            {
                int successCount = results.Values.Count(x => x == true);
                writer.WriteLine($"[{successCount}]/[{results.Count}]");
                foreach (var context in results)
                {
                    var text = context.Value ? "[+]" : "[-]";
                    writer.WriteLine($"{text}{context.Key}");
                }
                writer.Flush();
            }
        }
    }
}
