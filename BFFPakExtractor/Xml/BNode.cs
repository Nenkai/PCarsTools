using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BFFPakExtractor.Xml
{
    public class BNode
    {
        public int ElementIndex = -1;
        public BXmlFile ParentFile;
        public bool IsAttribute { get; set; }

        public BNode(int elementIndex, BXmlFile parentFile, bool isAttibute)
        {
            ElementIndex = elementIndex;
            ParentFile = parentFile;
            IsAttribute = isAttibute;
        }

        public BNode() { }

        public bool IsValid()
            => ElementIndex != -1 && ParentFile != null; 

        public BNode GetAttribute(string attrName)
        {
            if (IsAttribute)
                throw new Exception("Is already attribute");

            var element = ParentFile.GetElement(ElementIndex);
            if (element.Value.AttributeCount == 0)
                throw new Exception("Element has no attribute");

            var targetHash = BXmlFile.GetHash(attrName);
            for (int i = 0; i < element.Value.AttributeCount; i++)
            {
                var attr = ParentFile.GetAttribute(element.Value.AttributeIndex + i);
                if (attr.Value.AttributeHash == targetHash)
                    return new BNode(element.Value.AttributeIndex + i, ParentFile, true);
            }

            throw new Exception("Attribute not found");
            return new BNode();
        }

        public BNode GetFirstChildByName(string name)
        {
            if (IsAttribute)
                throw new Exception("Can't find a child on an attribute");

            if (!IsValid())
                return new BNode();

            var elmnt = ParentFile.GetElement(ElementIndex);
            if (elmnt is null)
                return new BNode();

            var potentialFirstChild = GetFirstChild();
            while (true)
            {
                if (!potentialFirstChild.IsValid())
                    return new BNode();

                if (potentialFirstChild.IsName(name))
                    return potentialFirstChild;

                potentialFirstChild = GetNextSibling();
            }

            return new BNode();
        }

        private BNode GetFirstChild()
        {
            if (IsAttribute)
                throw new Exception("Can't find a child on an attribute");

            if (!IsValid())
                return new BNode();

            var elmnt = ParentFile.GetElement(ElementIndex);
            if (elmnt is null)
                return new BNode();

            return new BNode(elmnt.Value.FirstChild, ParentFile, false);
        }

        public BNode GetNextSibling()
        {
            if (IsAttribute)
                throw new Exception("Can't find a sibling on an attribute");

            if (!IsValid())
                return new BNode();

            var elmnt = ParentFile.GetElement(ElementIndex);
            if (elmnt is null)
                return new BNode();

            if (elmnt.Value.NextSiblingIndex != -1)
                return new BNode(elmnt.Value.NextSiblingIndex, ParentFile, false);
            else
                return new BNode();
        }

        private bool IsName(string name)
        {
            if (!IsValid())
                return false;

            var hash = BXmlFile.GetHash(name);
            if (IsAttribute)
                return ParentFile.GetAttribute(this.ElementIndex)?.AttributeHash == hash;
            else
                return ParentFile.GetElement(this.ElementIndex)?.ElementHash == hash;
        }

        public bool GetNumber(int vectorIndex, out float number)
        {
            if (IsAttribute && IsValid())
                return ParentFile.GetNumber(ElementIndex, vectorIndex, out number);

            number = default;
            return false;
        }

        public bool GetString(int vectorIndex, out string str)
        {
            if (IsAttribute && IsValid())
                return ParentFile.GetString(ElementIndex, vectorIndex, out str);

            str = default;
            return false;
        }

        public bool GetStringBytes(int vectorIndex, out Memory<byte> str)
        {
            if (IsAttribute && IsValid())
                return ParentFile.GetStringBytes(ElementIndex, vectorIndex, out str);

            str = default;
            return false;
        }
    }
}
