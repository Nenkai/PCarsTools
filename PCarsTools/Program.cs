using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;

using CommandLine;
using CommandLine.Text;

using PCarsTools.Config;
using PCarsTools.Encryption;
using PCarsTools.Script;

namespace PCarsTools
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("PCarsTools 0.1.0 by Nenkai#9075");
            Console.WriteLine();

            Parser.Default.ParseArguments<TocVerbs, PakVerbs, DecryptScriptVerbs, BuildDatDecrypt>(args)
                .WithParsed<TocVerbs>(Toc)
                .WithParsed<PakVerbs>(Pak)
                .WithParsed<DecryptScriptVerbs>(DecryptScript)
                .WithParsed<BuildDatVerbs>(BuildDat)
                .WithNotParsed(HandleNotParsedArgs);
        }

        public static void Toc(TocVerbs options)
        {
            if (!File.Exists(options.InputPath))
            {
                Console.WriteLine($"File {options.InputPath} does not exist.");
                return;
            }

            if (!File.Exists(options.ConfigPath))
            {
                Console.WriteLine($"Config file {options.InputPath} does not exist.");
                return;
            }

            if (!BConfig.Instance.LoadConfig(options.ConfigPath))
            {
                Console.WriteLine($"Unable to load config file.");
                return;
            }

            BFileManager man = new BFileManager();
            if (!man.LoadFromCompressedToc(options.InputPath))
            {
                Console.WriteLine($"Unable to load toc file.");
                return;
            }

            Console.WriteLine("Paks in TOC:");
            for (int i = 0; i < man.Paks.Count; i++)
                Console.WriteLine($" - {man.Paks[i].Name} ({man.Paks[i].Entries.Count} entries, Encryption: {man.Paks[i].EncryptionType})");
            Console.WriteLine();

            if (options.UnpackAll)
            {
                Console.WriteLine("Unpacking files...");
                man.UnpackAll();
            }
        }

        public static void Pak(PakVerbs options)
        {
            if (!File.Exists(options.InputPath))
            {
                Console.WriteLine($"File {options.InputPath} does not exist.");
                return;
            }

            if (!File.Exists(options.ConfigPath))
            {
                Console.WriteLine($"Config file {options.InputPath} does not exist.");
                return;
            }

            if (!BConfig.Instance.LoadConfig(options.ConfigPath))
            {
                Console.WriteLine($"Unable to load config file.");
                return;
            }

            var pak = BPakFile.FromFile(options.InputPath);

            if (pak is null)
            {
                Console.WriteLine($"Unable to load pak file.");
                return;
            }

            if (string.IsNullOrEmpty(options.OutputPath))
                options.OutputPath = pak.Name;

            pak.UnpackAll(pak.Name);
        }

        public static void DecryptScript(DecryptScriptVerbs options)
        {
            if (!File.Exists(options.InputPath))
            {
                Console.WriteLine($"File {options.InputPath} does not exist.");
                return;
            }

            var bytes = File.ReadAllBytes(options.InputPath);
            ScriptDecrypt.Decrypt(bytes);
            File.WriteAllBytes(options.InputPath + ".dec", bytes);
            Console.WriteLine($"Decrypted.");
        }

        public static void BuildDat(BuildDatVerbs options)
        {
            var file = File.ReadAllBytes(options.InputPath);
            BuildDatDecrypt.Crypt(file);
            File.WriteAllBytes(options.InputPath + ".dec", file);
            Console.WriteLine($"Decrypted.");
        }

        public static void HandleNotParsedArgs(IEnumerable<Error> errors)
        {
            ;
        }

        [Verb("decryptscript", HelpText = "Decrypt a script file embedded in an executable as a bitmap.")]
        public class DecryptScriptVerbs
        {
            [Option('i', "input", Required = true, HelpText = "Input script file.")]
            public string InputPath { get; set; }
        }

        [Verb("toc", HelpText = "Unpacks a file system based on a toc file.")]
        public class TocVerbs
        {
            [Option('i', "input", Required = true, HelpText = "Input TOC file.")]
            public string InputPath { get; set; }

            [Option('c', "config", Required = true, HelpText = "Input config file. Should be languages/languages.bml. Needed to determine keys indexes.")]
            public string ConfigPath { get; set; }

            [Option('u', "unpack-all", HelpText = "Whether to unpack the whole file system.")]
            public bool UnpackAll { get; set; }
        }

        [Verb("pak", HelpText = "Unpacks files from a pak file.")]
        public class PakVerbs
        {
            [Option('i', "input", Required = true, HelpText = "Input TOC file.")]
            public string InputPath { get; set; }

            [Option('c', "config", Required = true, HelpText = "Input config file. Should be languages/languages.bml. Needed to determine keys indexes.")]
            public string ConfigPath { get; set; }

            [Option('o', "output", HelpText = "Output directory. Defaults to the pak file name.")]
            public string OutputPath { get; set; }

            [Option('u', "unpack-all", HelpText = "Whether to unpack the whole file system.")]
            public bool UnpackAll { get; set; }
        }

        [Verb("builddat", HelpText = "Decrypt a build.dat file.")]
        public class BuildDatVerbs
        {
            [Option('i', "input", Required = true, HelpText = "Input file.")]
            public string InputPath { get; set; }

            [Option('o', "output", Required = true, HelpText = "Output file.")]
            public string OutputPath { get; set; }
        }
    }
}
