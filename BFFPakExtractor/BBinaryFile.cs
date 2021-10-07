using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace BFFPakExtractor.Xml
{
    public class BBinaryFile
    {
        private byte[] _data;

        private Memory<byte> _headerPtr { get; set; }
        private Memory<byte> _dataPtr { get; set; }

        public const uint HeaderId = 0x594D4C42; // BLMY

        public BBinaryFile(byte[] data)
        {
            _data = data;
            _headerPtr = data;
            _dataPtr = data.AsMemory(0x10);
        }

        public Memory<byte> GetChunk(string chunkName)
        {
            uint id = GetId(chunkName);
            return GetChunk(id);
        }

        private Memory<byte> GetChunk(uint id)
        {
            var chunkHdr = GetChunkHeader(id);
            if (chunkHdr is not null)
                return _headerPtr.Slice((int)chunkHdr.Value.Offset);
            else
                return null;
        }

        private BChunkHeader? GetChunkHeader(uint chunkId)
        {
            int chunkCount = BinaryPrimitives.ReadInt32LittleEndian(_headerPtr.Span.Slice(0x04));
            for (int i = 0; i < chunkCount; i++)
            {
                var chunkPtr = _dataPtr.Slice(i * 0x10);
                var chunkHeader = MemoryMarshal.Cast<byte, BChunkHeader>(chunkPtr.Span)[0];

                if (chunkHeader.Id == chunkId)
                    return chunkHeader;
            }

            return null;
        }

        private bool IsValid()
        {
            BinaryPrimitives.TryReadInt32LittleEndian(_headerPtr.Span, out int val);
            return val == HeaderId;
        }

        public static uint GetId(string name)
        {
            int len = name.Length;
            uint res = 0;

            int maxLen = Math.Min(len, 4);
            for (int i = 0; i < maxLen; i++)
                res |= (uint)((byte)name[i] << (8 * i));

            return res;
        }
    }
}
