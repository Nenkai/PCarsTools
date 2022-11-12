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
using PCarsTools.Model;

namespace PCarsTools
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("PCarsTools 1.0.1 by Nenkai#9075");
            Console.WriteLine();

            Parser.Default.ParseArguments<TocVerbs, PakVerbs, DecryptScriptVerbs, BuildDatVerbs, DecryptModelVerbs>(args)
                .WithParsed<TocVerbs>(Toc)
                .WithParsed<PakVerbs>(Pak)
                .WithParsed<DecryptScriptVerbs>(DecryptScript)
                .WithParsed<BuildDatVerbs>(BuildDat)
                .WithParsed<DecryptModelVerbs>(DecryptModel)
                .WithNotParsed(HandleNotParsedArgs);
        }

        public static void Toc(TocVerbs options)
        {
            if (!File.Exists(options.InputPath))
            {
                Console.WriteLine($"File {options.InputPath} does not exist.");
                return;
            }

            if (!Directory.Exists(options.GameDirectory))
            {
                Console.WriteLine($"Game directory {options.InputPath} does not exist.");
                return;
            }

            string configFile = Path.Combine(options.GameDirectory, "Languages", "languages.bml");
            if (!File.Exists(configFile))
            {
                Console.WriteLine($"Required config file (Languages/languages.bml) does not exist in game directory.");
                return;
            }

            if (!BConfig.Instance.LoadConfig(configFile))
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
                man.UnpackAll(options.GameDirectory);
            }
        }

        public static void Pak(PakVerbs options)
        {
            if (!File.Exists(options.InputPath))
            {
                Console.WriteLine($"File {options.InputPath} does not exist.");
                return;
            }

            if (!Directory.Exists(options.GameDirectory))
            {
                Console.WriteLine($"Game directory {options.InputPath} does not exist.");
                return;
            }

            string configFile = Path.Combine(options.GameDirectory, "Languages", "languages.bml");
            if (!File.Exists(configFile))
            {
                Console.WriteLine($"Required config file (Languages/languages.bml) does not exist in game directory.");
                return;
            }

            if (!BConfig.Instance.LoadConfig(configFile))
            {
                Console.WriteLine($"Unable to load config file.");
                return;
            }

            var pak = BPakFile.FromFile(options.InputPath, withExtraInfo: true);

            if (pak is null)
            {
                Console.WriteLine($"Unable to load pak file.");
                return;
            }

            if (string.IsNullOrEmpty(options.OutputPath))
            {
                options.OutputPath = Path.Combine(Path.GetDirectoryName(options.InputPath), pak.Name + "_extracted");
                Directory.CreateDirectory(options.OutputPath);
            }
            pak.UnpackAll(options.OutputPath);
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

        public static void DecryptModel(DecryptModelVerbs options)
        {
            MeshBinary.Load(options.InputPath);
            Console.WriteLine("Model decrypted/encrypted.");
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

            [Option('g', "game-dir", Required = true, HelpText = "Input game directory.")]
            public string GameDirectory { get; set; }

            [Option('o', "output")]
            public string OutputDirectory { get; set; }

            [Option('u', "unpack-all", HelpText = "Whether to unpack the whole toc. If not provided, nothing will be extracted.")]
            public bool UnpackAll { get; set; }
        }

        [Verb("pak", HelpText = "Unpacks files from a pak file.")]
        public class PakVerbs
        {
            [Option('i', "input", Required = true, HelpText = "Input TOC file.")]
            public string InputPath { get; set; }

            [Option('g', "game-dir", Required = true, HelpText = "Input game directory.")]
            public string GameDirectory { get; set; }

            [Option('o', "output", HelpText = "Output directory. Defaults to the pak file name.")]
            public string OutputPath { get; set; }
        }

        [Verb("build-dat", HelpText = "Decrypt a build.dat file.")]
        public class BuildDatVerbs
        {
            [Option('i', "input", Required = true, HelpText = "Input file.")]
            public string InputPath { get; set; }
        }

        [Verb("decryptmodel", HelpText = "Decrypts model files (.meb)")]
        public class DecryptModelVerbs
        {
            [Option('i', "input", Required = true, HelpText = "Input file.")]
            public string InputPath { get; set; }
        }
    }
}
