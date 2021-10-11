﻿using System;
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
            Parser.Default.ParseArguments<DecryptScriptVerbs, TocVerbs>(args)
                .WithParsed<TocVerbs>(Toc)
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
            for(int i = 0; i < man.Paks.Count; i++)
                Console.WriteLine($" - {man.Paks[i].Name} ({man.Paks[i].Entries.Count} entries, Encryption: {man.Paks[i].EncryptionType})");
            Console.WriteLine();

            if (options.UnpackAll)
            {
                Console.WriteLine("Unpacking files...");
                man.UnpackAll();
            }
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
        }

        
        public static void BuildDat(BuildDatVerbs options)
        {
            var file = File.ReadAllBytes(options.InputPath);
            BuildDatDecrypt.Crypt(file);
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