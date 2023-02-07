using System;
using System.IO;
using System.Text;

namespace AoP.Editor
{
    public sealed class FileTgaTx
    {

        public readonly HeaderData headerData;
        public readonly TextureData textureData;

        public FileTgaTx(string path)
        {
            using FileStream fs = File.OpenRead(path);
            using BinaryReader br = new(fs, Encoding.ASCII);
            headerData = new HeaderData(br);
            textureData = new TextureData(br, headerData);
        }

        public sealed class HeaderData
        {
            public readonly int flags;
            public readonly int width;
            public readonly int height;
            public readonly int mipMapCount;
            public readonly string fourCC;
            public readonly int pitchOrLinearSize;

            public HeaderData(BinaryReader br)
            {
                flags = br.ReadInt32();
                width = br.ReadInt32();
                height = br.ReadInt32();
                mipMapCount = br.ReadInt32();
                fourCC = new string(br.ReadChars(4));
                pitchOrLinearSize = br.ReadInt32();
            }
        }

        public sealed class TextureData
        {
            public readonly byte[] data;

            public TextureData(BinaryReader br, HeaderData header)
            {
                data = br.ReadBytes((int)SumGeometricProgression(header.pitchOrLinearSize, 0.25, header.mipMapCount));
            }

            private double SumGeometricProgression(double b1, double q, int n)
            {
                return b1 * (1.0 - Math.Pow(q, n)) / (1.0 - q);
            }
        }
    }
}
