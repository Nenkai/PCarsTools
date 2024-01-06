using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Syroot.BinaryData;
using System.Numerics;
using System.Buffers.Binary;

using PCarsTools.Encryption;

namespace PCarsTools.Model
{
    public class MeshBinary
    {
        public static void Load(string fileName)
        {
			RemovePC3Padding(fileName);
			
            using var fs = new FileStream(fileName, FileMode.Open);
            using var bs = new BinaryStream(fs);

            uint encryptionKey = BHashCode.CreateHash32NoCase(Path.GetFileName(fileName));

            BVersion version = new BVersion(bs.ReadUInt32());

            bool HasBones = false;
            if (version.Minor == 2)
            {
                HasBones = bs.ReadBoolean();
            }
            else if (version.Minor == 4 || version.Minor == 5)
            {
                HasBones = bs.ReadBoolean(); ;
                bool DynamicEnvMapRequired = bs.ReadBoolean();
                bs.Position += 2;
            }
            else if (version.Minor == 6)
            {
                HasBones = bs.ReadBoolean();
                bool DynamicEnvMapRequired = bs.ReadBoolean();
                byte Flags = bs.Read1Byte();
                byte cpuReason = bs.Read1Byte();
                byte empty = bs.Read1Byte();
                byte unkBool = bs.Read1Byte();
                bs.Position += 2;
            }
            else if (version.Minor >= 7)
            {
                HasBones = bs.ReadBoolean();
                bool DynamicEnvMapRequired = bs.ReadBoolean();
                byte Flags = bs.Read1Byte();
                byte cpuReason = bs.Read1Byte();
                byte empty = bs.Read1Byte();
                byte unkBool = bs.Read1Byte();
                bs.Position += 2;
            }

            string name = bs.ReadString(StringCoding.ZeroTerminated);
            Console.WriteLine($"Mesh Binary Name: {name}");

            if (version.Minor >= 4)
                bs.Align(0x04);

            int NumVertsPerStream = bs.ReadInt32();
            int NumStreams = bs.ReadInt32();
            int NumIndexBuffers = bs.ReadInt32();
            bs.Position += 0x10; // localBoundsSphere BVec4F
            float boundingAABBMin_x = bs.ReadInt32();
            float boundingAABBMin_y = bs.ReadInt32();
            float boundingAABBMin_z = bs.ReadInt32();
            float boundingAABBMax_x = bs.ReadInt32();
            float boundingAABBMax_y = bs.ReadInt32();
            float boundingAABBMax_z = bs.ReadInt32();

            Console.WriteLine($"Verts per stream: {NumVertsPerStream}");

            if (HasBones && version.Minor >= 2)
            {
                int numBones = bs.ReadInt32();
                int boneStrSize = bs.ReadInt32();
                string[] boneNames = bs.ReadStrings(numBones, StringCoding.ZeroTerminated);
                bs.Align(0x04);

                bs.Position += 0x10 * numBones; // referenceTransforms Vec4F
                bs.Position += 0x10 * numBones; // boneReferenceInverseTransforms Vec4F
                bs.Position += 0x10 * numBones; // unk Vec4F
            }
    
            // This is normally const expr'd
            int baseXor = (int)MathF.Floor(MathF.Log10(1.49f) * 1000.0f) |
                          ((int)MathF.Floor(MathF.Tan(0.418f) * 500.0f) << 8) |
                          ((int)MathF.Floor(MathF.Cos(1.3561f) * 1000.0f) << 16) |
                          ((int)MathF.Floor(MathF.Sin(0.74f) * 100.0f) + (int)MathF.Floor(MathF.Sin(0.45f) * 100.0f) << 24);

            long positionFormatOffset = 0;
            for (var i = 0; i < NumStreams; i++)
            {
                positionFormatOffset = bs.Position;
                DXGIFORMAT format = (DXGIFORMAT)bs.ReadInt32();
                SemanticName semantic = (SemanticName)bs.ReadInt32();

                if (semantic != SemanticName.POSITION)
                    throw new Exception("Expected POSITION stream in meb as first entry");

                if (format != DXGIFORMAT.DXGI_FORMAT_D32_FLOAT_S8X24_UINT)
                {
                    Console.WriteLine($"POSITION stream format is not {DXGIFORMAT.DXGI_FORMAT_D32_FLOAT_S8X24_UINT}, model vertices are not encrypted.");
                    return;
                }    

                int semanticIndex = bs.ReadInt32();

                for (var j = 0; j < NumVertsPerStream; j++)
                {
                    if (format == DXGIFORMAT.DXGI_FORMAT_D32_FLOAT_S8X24_UINT)
                    {
                        // Decrypt X axis
                        uint cXor = encryptionKey ^ (uint)baseXor;
                        cXor ^= (uint)j;
                        uint xEnc = bs.ReadUInt32();
                        xEnc ^= BitOperations.RotateLeft(cXor, j % 32);

                        float x = BitConverter.Int32BitsToSingle((int)xEnc);
                        bs.Position -= 4;
                        bs.WriteSingle(x);
                    }
                    else
                    {
                        float x = bs.ReadSingle();
                    }

                    float y = bs.ReadSingle();
                    float z = bs.ReadSingle();
                }

                // Our job here is done
                break;
            }

            // Change format to decrypted floats
            if (positionFormatOffset != 0)
            {
                bs.Position = positionFormatOffset;
                bs.WriteInt32((int)DXGIFORMAT.DXGI_FORMAT_R32G32B32A32_FLOAT);
            }
        }

        private static void RemovePC3Padding(string fileName)
        {
            using var fs = new FileStream(fileName, FileMode.Open);
            using var bs = new BinaryStream(fs);

            byte[] before = bs.ReadBytes(8);
            int pad = bs.ReadInt32();
            if (pad != 0)
            {
                bs.Close();
                fs.Close();
                return;
            }

            FileInfo file = new FileInfo(fileName);
            byte[] after = bs.ReadBytes((int)file.Length - 12);
            bs.Seek(8, SeekOrigin.Begin);
            bs.Write(after);

            fs.SetLength((int)file.Length - 4);

            bs.Flush();
            bs.Close();
            fs.Close();
        }

        enum SemanticName : int
        {
            POSITION,
            BLENDWEIGHT,
            NORMAL,
            TEXCOORD,
            TANGENT,
            BINORMAL,
            COLOR,
            DEPTH,
            BLENDINDICES,
        }

        enum DXGIFORMAT : int
        {
            DXGI_FORMAT_UNKNOWN,
            DXGI_FORMAT_R32G32B32A32_TYPELESS,
            DXGI_FORMAT_R32G32B32A32_FLOAT,
            DXGI_FORMAT_R32G32B32A32_UINT,
            DXGI_FORMAT_R32G32B32A32_SINT,
            DXGI_FORMAT_R32G32B32_TYPELESS,
            DXGI_FORMAT_R32G32B32_FLOAT,
            DXGI_FORMAT_R32G32B32_UINT,
            DXGI_FORMAT_R32G32B32_SINT,
            DXGI_FORMAT_R16G16B16A16_TYPELESS,
            DXGI_FORMAT_R16G16B16A16_FLOAT,
            DXGI_FORMAT_R16G16B16A16_UNORM,
            DXGI_FORMAT_R16G16B16A16_UINT,
            DXGI_FORMAT_R16G16B16A16_SNORM,
            DXGI_FORMAT_R16G16B16A16_SINT,
            DXGI_FORMAT_R32G32_TYPELESS,
            DXGI_FORMAT_R32G32_FLOAT,
            DXGI_FORMAT_R32G32_UINT,
            DXGI_FORMAT_R32G32_SINT,
            DXGI_FORMAT_R32G8X24_TYPELESS,
            DXGI_FORMAT_D32_FLOAT_S8X24_UINT,
            DXGI_FORMAT_R32_FLOAT_X8X24_TYPELESS,
            DXGI_FORMAT_X32_TYPELESS_G8X24_UINT,
            DXGI_FORMAT_R10G10B10A2_TYPELESS,
            DXGI_FORMAT_R10G10B10A2_UNORM,
            DXGI_FORMAT_R10G10B10A2_UINT,
            DXGI_FORMAT_R11G11B10_FLOAT,
            DXGI_FORMAT_R8G8B8A8_TYPELESS,
            DXGI_FORMAT_R8G8B8A8_UNORM,
            DXGI_FORMAT_R8G8B8A8_UNORM_SRGB,
            DXGI_FORMAT_R8G8B8A8_UINT,
            DXGI_FORMAT_R8G8B8A8_SNORM,
            DXGI_FORMAT_R8G8B8A8_SINT,
            DXGI_FORMAT_R16G16_TYPELESS,
            DXGI_FORMAT_R16G16_FLOAT,
            DXGI_FORMAT_R16G16_UNORM,
            DXGI_FORMAT_R16G16_UINT,
            DXGI_FORMAT_R16G16_SNORM,
            DXGI_FORMAT_R16G16_SINT,
            DXGI_FORMAT_R32_TYPELESS,
            DXGI_FORMAT_D32_FLOAT,
            DXGI_FORMAT_R32_FLOAT,
            DXGI_FORMAT_R32_UINT,
            DXGI_FORMAT_R32_SINT,
            DXGI_FORMAT_R24G8_TYPELESS,
            DXGI_FORMAT_D24_UNORM_S8_UINT,
            DXGI_FORMAT_R24_UNORM_X8_TYPELESS,
            DXGI_FORMAT_X24_TYPELESS_G8_UINT,
            DXGI_FORMAT_R8G8_TYPELESS,
            DXGI_FORMAT_R8G8_UNORM,
            DXGI_FORMAT_R8G8_UINT,
            DXGI_FORMAT_R8G8_SNORM,
            DXGI_FORMAT_R8G8_SINT,
            DXGI_FORMAT_R16_TYPELESS,
            DXGI_FORMAT_R16_FLOAT,
            DXGI_FORMAT_D16_UNORM,
            DXGI_FORMAT_R16_UNORM,
            DXGI_FORMAT_R16_UINT,
            DXGI_FORMAT_R16_SNORM,
            DXGI_FORMAT_R16_SINT,
            DXGI_FORMAT_R8_TYPELESS,
            DXGI_FORMAT_R8_UNORM,
            DXGI_FORMAT_R8_UINT,
            DXGI_FORMAT_R8_SNORM,
            DXGI_FORMAT_R8_SINT,
            DXGI_FORMAT_A8_UNORM,
            DXGI_FORMAT_R1_UNORM,
            DXGI_FORMAT_R9G9B9E5_SHAREDEXP,
            DXGI_FORMAT_R8G8_B8G8_UNORM,
            DXGI_FORMAT_G8R8_G8B8_UNORM,
            DXGI_FORMAT_BC1_TYPELESS,
            DXGI_FORMAT_BC1_UNORM,
            DXGI_FORMAT_BC1_UNORM_SRGB,
            DXGI_FORMAT_BC2_TYPELESS,
            DXGI_FORMAT_BC2_UNORM,
            DXGI_FORMAT_BC2_UNORM_SRGB,
            DXGI_FORMAT_BC3_TYPELESS,
            DXGI_FORMAT_BC3_UNORM,
            DXGI_FORMAT_BC3_UNORM_SRGB,
            DXGI_FORMAT_BC4_TYPELESS,
            DXGI_FORMAT_BC4_UNORM,
            DXGI_FORMAT_BC4_SNORM,
            DXGI_FORMAT_BC5_TYPELESS,
            DXGI_FORMAT_BC5_UNORM,
            DXGI_FORMAT_BC5_SNORM,
            DXGI_FORMAT_B5G6R5_UNORM,
            DXGI_FORMAT_B5G5R5A1_UNORM,
            DXGI_FORMAT_B8G8R8A8_UNORM,
            DXGI_FORMAT_B8G8R8X8_UNORM,
            DXGI_FORMAT_R10G10B10_XR_BIAS_A2_UNORM,
            DXGI_FORMAT_B8G8R8A8_TYPELESS,
            DXGI_FORMAT_B8G8R8A8_UNORM_SRGB,
            DXGI_FORMAT_B8G8R8X8_TYPELESS,
            DXGI_FORMAT_B8G8R8X8_UNORM_SRGB,
            DXGI_FORMAT_BC6H_TYPELESS,
            DXGI_FORMAT_BC6H_UF16,
            DXGI_FORMAT_BC6H_SF16,
            DXGI_FORMAT_BC7_TYPELESS,
            DXGI_FORMAT_BC7_UNORM,
            DXGI_FORMAT_BC7_UNORM_SRGB,
            DXGI_FORMAT_AYUV,
            DXGI_FORMAT_Y410,
            DXGI_FORMAT_Y416,
            DXGI_FORMAT_NV12,
            DXGI_FORMAT_P010,
            DXGI_FORMAT_P016,
            DXGI_FORMAT_420_OPAQUE,
            DXGI_FORMAT_YUY2,
            DXGI_FORMAT_Y210,
            DXGI_FORMAT_Y216,
            DXGI_FORMAT_NV11,
            DXGI_FORMAT_AI44,
            DXGI_FORMAT_IA44,
            DXGI_FORMAT_P8,
            DXGI_FORMAT_A8P8,
            DXGI_FORMAT_B4G4R4A4_UNORM,
            DXGI_FORMAT_P208,
            DXGI_FORMAT_V208,
            DXGI_FORMAT_V408,
            DXGI_FORMAT_SAMPLER_FEEDBACK_MIN_MIP_OPAQUE,
            DXGI_FORMAT_SAMPLER_FEEDBACK_MIP_REGION_USED_OPAQUE,
            DXGI_FORMAT_FORCE_UINT
        }
    }
}
