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
using PCarsTools.Texture;
using PCarsTools.Pak;

using Syroot.BinaryData;

namespace PCarsTools
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("PCarsTools 1.1.3 by Nenkai");
            Console.WriteLine();

            Parser.Default.ParseArguments<TocVerbs, PakVerbs, DecryptScriptVerbs, BuildDatVerbs, DecryptModelVerbs, ConvertTextureVerbs>(args)
                .WithParsed<TocVerbs>(Toc)
                .WithParsed<PakVerbs>(Pak)
                .WithParsed<DecryptScriptVerbs>(DecryptScript)
                .WithParsed<BuildDatVerbs>(BuildDat)
                .WithParsed<DecryptModelVerbs>(DecryptModel)
                .WithParsed<ConvertTextureVerbs>(ConvertTexture)
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

            if (options.KeysetType != KeysetType.PC2AndAbove)
                BPakFileEncryption.SetKeyset(options.KeysetType);

            Console.WriteLine("Paks in TOC:");
            for (int i = 0; i < man.Paks.Count; i++)
                Console.WriteLine($" - {man.Paks[i].Header.mFileName} ({man.Paks[i].Entries.Count} entries, Encryption: {man.Paks[i].Header.mEncryption})");
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

            BPakFileEncryption.SetKeyset(options.KeysetType);

            var pak = new BPakFile();
            pak.FromFile(options.InputPath, withExtraInfo: true);

            if (pak is null)
            {
                Console.WriteLine($"Unable to load pak file.");
                return;
            }

            if (string.IsNullOrEmpty(options.OutputPath))
            {
                options.OutputPath = Path.Combine(Path.GetDirectoryName(options.InputPath), pak.Header.mFileName + "_extracted");
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
            MeshBinary.LoadAndDecrypt(options.InputPath);
            Console.WriteLine("Model decrypted/encrypted.");
        }
        
        public static void ConvertTexture(ConvertTextureVerbs options)
        {
            foreach (var file in options.Files)
            {
                try
                {
                    TextureFile.RemovePC3Padding(file);
                    Console.WriteLine($"Converted {file}.");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Could not convert {file}: {e.Message}.");
                }
            }
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

        [Verb("toc", HelpText = "Unpacks a file system based on a toc file.\n" +
            "NOTE: Use the --game-type argument when unpacking from Project Cars 1 or Test Drive Ferrari Racing Legends!")]
        public class TocVerbs
        {
            [Option('i', "input", Required = true, HelpText = "Input TOC file (files from TOCFiles, or compressed.toc file from PC GO)")]
            public string InputPath { get; set; }

            [Option('g', "game-dir", Required = true, HelpText = "Input game directory.")]
            public string GameDirectory { get; set; }

            [Option('o', "output")]
            public string OutputDirectory { get; set; }

            [Option('u', "unpack-all", HelpText = "Whether to unpack the whole toc. If not provided, nothing will be extracted.")]
            public bool UnpackAll { get; set; }

            [Option("game-type", HelpText = "Use this if unpacking from games earlier than Project Cars 2 (different encryption keys)\n" +
                "Options: \n" +
                "- PC1 (Project Cars 1)\n" +
                "- TDFRL (Test Drive Ferrari Racing Legends)")]
            public KeysetType KeysetType { get; set; } = KeysetType.PC2AndAbove;
        }

        [Verb("pak", HelpText = "Unpacks files from a pak aka .bff file.\n" +
            "NOTE: Use the --game-type argument when unpacking from Project Cars 1 or Test Drive Ferrari Racing Legends!")]
        public class PakVerbs
        {
            [Option('i', "input", Required = true, HelpText = "Input PAK/.bff file.")]
            public string InputPath { get; set; }

            [Option('g', "game-dir", Required = true, HelpText = "Input game directory.")]
            public string GameDirectory { get; set; }

            [Option('o', "output", HelpText = "Output directory. Defaults to the pak file name.")]
            public string OutputPath { get; set; }

            [Option("game-type", HelpText = "Use this if unpacking from games earlier than Project Cars 2 (different encryption keys)\n" +
                "Options: \n" +
                "- PC1 (Project Cars 1)\n" +
                "- TDFRL (Test Drive Ferrari Racing Legends)")]
            public KeysetType KeysetType { get; set; } = KeysetType.PC2AndAbove;
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
        
        [Verb("convert-texture", HelpText = "Converts texture files by removing the header (.tex)")]
        public class ConvertTextureVerbs
        {
            [Option('i', "input", Required = true, HelpText = "Input file(s).")]
            public IEnumerable<string> Files { get; set; }
        }
    }
}
