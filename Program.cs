using System;
using System.Linq;

foreach (var file in FindFilesFast.Finder.FindFiles(".", true).Take(100)) 
{
    Console.WriteLine($"{file.FullName}\t{file.IsDirectory}\t{file.FileSize}");
}