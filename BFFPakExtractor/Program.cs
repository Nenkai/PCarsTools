using System;

using BFFPakExtractor.Config;

namespace BFFPakExtractor
{
    class Program
    {
        static void Main(string[] args)
        {

            BConfig.Instance.LoadConfig(@"languages.bml");

            BFileManager man = new BFileManager();
            man.LoadFromCompressedToc("compressed.toc");
        }
    }
}
