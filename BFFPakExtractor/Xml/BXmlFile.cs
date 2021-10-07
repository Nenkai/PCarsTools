using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;
using System.Buffers.Binary;

using Syroot.BinaryData;
using Syroot.BinaryData.Memory;

namespace BFFPakExtractor.Xml
{
    public class BXmlFile
    {
        private BBinaryFile _binFile;

        public BHeader? HeaderChunk;
        public Memory<BCollection>? CollectionChunk;
        public Memory<BElement>? ElementChunk;
        public Memory<BAttribute>? AttributeChunk;
        public Memory<float>? NumberChunk;
        public Memory<byte>? StringChunk;

        private static Dictionary<uint, string> HashTable = new Dictionary<uint, string>();
        private static string[] StringSymbols = new string[]
        {
            // Config file
            "Languages",
            "TextLanguages",
            "AudioLanguages",
            "VideoLanguages",
            "Video",
            "Territory",
            "Encryption",
            "Pattern",
            // Collections
            "Languages/TextLanguages",
            "Languages/AudioLanguages",
            "Languages/VideoLanguages",
            "Languages/Territory",
            "Languages/Video",
            "Languages/Encryption",
            "Languages/Pattern",

            "Type",
            "Count",
            "Language",
            "Name",
            "Index",
            "Data",

            // LoadMaterial
            "shader",
            "technique",
            "numparams",
            "name",
            "type",
            "v",
            "t",
            "value",

            // CollectHierarchyUserFlags
            "subobjects",
            "NODE",
            "userflags",

            // LoadLightsFromSGX
            "SCENE",
            "OBJ_ID",
            "no",
            "LIGHT",
            "UID",
            "Direction",
            "Colour",
            "Intensity",
            "Range",
            "InnerAngle",
            "OuterAngle",

            "OCCLUDER",
            "TRANSFORM",
            "Position",
            "Orientation",
            "PositionTL",
            "PositionTR",
            "PositionBR",
            "PositionBL",
            "Resource",
            "MATRIX",
            "id",
            "Offset",
            "Scale",
            "parent",
            "MatrixNumber",
            "Filename",
            "nearDistances",
            "farDistances",
            "CONTROL",

            // CMaterialSpec::CreateFromBinaryFile
            "material",
            "version",
            "material/define",
            "material/shaderparam",
            "technique",
            "row",
            "r",

            "Reflection"
        };

        static BXmlFile()
        {
            foreach (var str in StringSymbols)
            {
                uint hash = GetHash(str);
                if (!HashTable.ContainsKey(hash))
                    HashTable.Add(hash, str);
            }
        }

        public void Load(string fileName)
        {
            var file = File.ReadAllBytes(fileName);
            _binFile = new BBinaryFile(file);
        }

        public void Load(byte[] data)
        {
            _binFile = new BBinaryFile(data);
        }

        public BNode GetNodes(string nodePath)
        {
            BCollection? collection = GetCollection(nodePath);
            if (collection is null)
                return new BNode();

            return new BNode(collection.Value.Index, this, collection.Value.IsAttribute);
        }

        #region Chunk Fetchers
        private BHeader? GetHeader()
        {
            if (HeaderChunk is null)
            {
                Memory<byte>? data = _binFile.GetChunk("HEAD");
                if (data is null)
                    return null;
                HeaderChunk = MemoryMarshal.Cast<byte, BHeader>(data.Value.Span)[0];
            }

            return HeaderChunk;
        }

        private Memory<BCollection>? GetCollections()
        {
            if (CollectionChunk is null)
            {
                Memory<byte>? data = _binFile.GetChunk("COLL");
                if (data is null)
                    return null;
                var tmp = MemoryMarshal.Cast<byte, BCollection>(data.Value.Span);
                CollectionChunk = tmp.ToArray().AsMemory(); // Ew.
            }

            return CollectionChunk;
        }

        private Memory<BElement>? GetElements()
        {
            if (ElementChunk is null)
            {
                Memory<byte>? data = _binFile.GetChunk("ELMT");
                if (data is null)
                    return null;
                var tmp = MemoryMarshal.Cast<byte, BElement>(data.Value.Span);
                ElementChunk = tmp.ToArray().AsMemory(); // Ew.
            }

            return ElementChunk;
        }

        private Memory<BAttribute>? GetAttributes()
        {
            if (AttributeChunk is null)
            {
                Memory<byte>? data = _binFile.GetChunk("ATTR");
                if (data is null)
                    return null;
                var tmp = MemoryMarshal.Cast<byte, BAttribute>(data.Value.Span);
                AttributeChunk = tmp.ToArray().AsMemory(); // Ew.
            }

            return AttributeChunk;
        }

        private Memory<float>? GetNumbers()
        {
            if (NumberChunk is null)
            {
                Memory<byte>? data = _binFile.GetChunk("NUMB");
                if (data is null)
                    return null;
                var tmp = MemoryMarshal.Cast<byte, float>(data.Value.Span);
                NumberChunk = tmp.ToArray().AsMemory(); // Ew.
            }

            return NumberChunk;
        }

        private Memory<byte>? GetStrings()
        {
            if (StringChunk is null)
            {
                Memory<byte>? data = _binFile.GetChunk("STRS");
                if (data is null)
                    return null;

                StringChunk = data;
            }

            return StringChunk;
        }

        #endregion

        private BCollection? GetCollection(string nodePath)
        {
            uint hash = GetHash(nodePath);
            return GetCollection(hash);
        }

        private BCollection? GetCollection(uint hash)
        {
            var header = GetHeader();
            var collections = GetCollections();

            if (header is null || collections is null)
                return null;

            // BSearch it
            int min = 0;
            int max = (int)(header.Value.mCollectionCount - 1);
            while (min <= max)
            {
                int mid = (max + min) / 2;
                BCollection current = CollectionChunk.Value.Span[mid];
                uint cHash = current.Hash;
                if (current.Hash == hash)
                    return current;

                if (hash >= cHash)
                    min = mid + 1;
                else if (hash < cHash)
                    max = mid - 1;
            }

            return null;
        }

        public BElement? GetElement(int elementIndex)
        {
            var header = GetHeader();
            var elements = GetElements();

            if (header is null || elements is null)
                return null;

            if (elementIndex < 0 || elementIndex > header.Value.mElementCount)
                throw new IndexOutOfRangeException("Element index too big or small");

            return elements.Value.Span[elementIndex];
        }

        public BAttribute? GetAttribute(int attributeIndex)
        {
            var header = GetHeader();
            var attributes = GetAttributes();

            if (header is null || attributes is null)
                return null;

            if (attributeIndex < 0 || attributeIndex > header.Value.mAttributeCount)
                throw new IndexOutOfRangeException("Element index too big or small");

            return attributes.Value.Span[(int)attributeIndex];
        }

        public bool GetNumber(int attributeIndex, int vectorIndex, out float number)
        {
            number = default;
            if (!VerifyAttribute(attributeIndex, vectorIndex, AttributeType.Number))
                return false;

            var attr = GetAttribute(attributeIndex);
            var numbers = GetNumbers();

            number = numbers.Value.Span[attr.Value.Value + vectorIndex];
            return true;
        }

        public bool GetString(int attributeIndex, int vectorIndex, out string str)
        {
            str = default;
            if (!VerifyAttribute(attributeIndex, vectorIndex, AttributeType.String))
                return false;

            var attr = GetAttribute(attributeIndex);
            var strs = GetStrings();

            str = GetNullTerminatedString(strs.Value.Span.Slice(attr.Value.Value));
            return true;
        }

        public bool GetStringBytes(int attributeIndex, int vectorIndex, out Memory<byte> str)
        {
            str = default;
            if (!VerifyAttribute(attributeIndex, vectorIndex, AttributeType.String))
                return false;

            var attr = GetAttribute(attributeIndex);
            var strs = GetStrings();

            str = GetNullTerminatedStringBytes(strs.Value.Slice(attr.Value.Value));
            return true;
        }

        public bool VerifyAttribute(int attrIndex, int vectorIndex, AttributeType attributeType)
        {
            var attr = GetAttribute(attrIndex);
            if (attr is null)
                return false;

            if (!attr.Value.IsType(attributeType))
                return false;

            if (vectorIndex >= 0 && vectorIndex <= attr.Value.ValueVectorCount)
                return true;
            else
                throw new IndexOutOfRangeException("Out of range");

            return false;
        }

        public static uint GetHash(string fileName)
        {
            uint result = 0;
            for (int i = 0; i < fileName.Length; i++)
            {
                result = 31 * ((result >> 27) + 32 * result);
                result += fileName[i];
            }

            return result;
        }

        public void Dump()
        {
            var header = GetHeader();
            int rootNode = header.Value.RootNodeIndex;

            var rootElem = GetElement(rootNode);
            TraverseElement(rootElem.Value);
        }

        private void TraverseElement(BElement elem)
        {
            string elemName = string.Empty;
            if (HashTable.ContainsKey(elem.ElementHash))
                elemName = HashTable[elem.ElementHash];
            else
                Console.WriteLine($"Could not find hash name 0x{elem.ElementHash:X8}");
            Console.WriteLine(elemName);

            for (int i = 0; i < elem.AttributeCount; i++)
            {
                var attr = GetAttribute(elem.AttributeIndex + i).Value;
                string attrName = string.Empty;
                if (HashTable.ContainsKey(attr.AttributeHash))
                    attrName = HashTable[attr.AttributeHash];
                else
                    Console.WriteLine($"Could not find hash name 0x{attr.AttributeHash:X8}");

                Console.WriteLine($"A:{attrName}");
                if (attr.ValueType == AttributeType.String)
                {
                    var strs = GetStrings();
                    var strBytes = strs.Value.Slice(attr.Value);
                    var str = GetNullTerminatedString(strBytes.Span);
                    Console.WriteLine($"AV:{str}");
                }
                else if (attr.ValueType == AttributeType.Number)
                {
                    var numbs = GetNumbers();
                    var attrVal = numbs.Value.Span[attr.Value];
                    Console.WriteLine($"AV:{attrVal}");
                }
            }

            if (elem.ChildCount > 0)
            {
                var childElement = GetElement(elem.FirstChild);
                TraverseElement(childElement.Value);
            }

            if (elem.NextSiblingIndex != -1)
            {
                var nextElement = GetElement(elem.NextSiblingIndex);
                TraverseElement(nextElement.Value);
            }            
        }

        private string GetNullTerminatedString(Span<byte> bytes)
        {
            for (int i = 0; i< bytes.Length; i++)
            {
                if (bytes[i] == 0)
                    return Encoding.UTF8.GetString(bytes.Slice(0, i)).ToString();
            }

            return null;
        }

        private Memory<byte> GetNullTerminatedStringBytes(Memory<byte> bytes)
        {
            for (int i = 0; i < bytes.Length; i++)
            {
                if (bytes.Span[i] == 0)
                    return bytes.Slice(0, i);
            }

            return null;
        }
    }
}
