using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoLibrary;
using System.IO;

namespace YoutubeDL
{
    class Program
    {
        static string ROOT = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        static void Main(string[] args)
        {
            Console.WriteLine("URL:");
            var url = Console.ReadLine();

            var youtube = YouTube.Default;

            var video = youtube.GetVideo(url);

            var fileName = $"{video.Title}.{video.FileExtension}";
            var path = Path.Combine(ROOT, fileName);

            Console.WriteLine($"Download:{video.Title}");
            Console.WriteLine($"Save to:{path}");

            File.WriteAllBytes(path, video.GetBytes());

            Console.WriteLine("Done");
        }
    }
}
