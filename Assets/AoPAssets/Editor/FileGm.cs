using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;

namespace AoP.Editor
{
    public sealed class FileGm
    {
        public readonly string name;
        public readonly string nameWithoutExtension;
        public readonly string path;
        public readonly HeaderData headerData;
        public readonly StringsData stringsData;
        public readonly TexturesData texturesData;
        public readonly MaterialsData materialsData;
        public readonly LocatorsData locatorsData;
        public readonly MeshObjectsData meshObjectsData;
        public readonly TrianglesData trianglesData;
        public readonly VertexBuffersData vertexBuffersData;
        public readonly VerticesData verticesData;

        public FileGm(string path)
        {
            name = Path.GetFileName(path);
            nameWithoutExtension = Path.GetFileNameWithoutExtension(path);
            this.path = path;
            using FileStream fs = File.OpenRead(path);
            using BinaryReader br = new(fs, Encoding.ASCII);
            headerData = new HeaderData(br);
            stringsData = new StringsData(br, headerData.stringsDataLength, headerData.stringsDataItemCount);
            texturesData = new TexturesData(br, headerData.texturesCount, stringsData);
            materialsData = new MaterialsData(br, headerData.version, headerData.materialsCount, stringsData);
            locatorsData = new LocatorsData(br, headerData.locatorsCount, stringsData);
            meshObjectsData = new MeshObjectsData(br, headerData.version, headerData.meshObjectsCount, stringsData);
            trianglesData = new TrianglesData(br, headerData.trianglesCount);
            vertexBuffersData = new VertexBuffersData(br, headerData.vertexBuffersCount);
            verticesData = new VerticesData(br, vertexBuffersData.vertexBuffers);
        }

        public sealed class HeaderData
        {
            public readonly string version;
            public readonly int flags;
            public readonly int stringsDataLength;
            public readonly int stringsDataItemCount;
            public readonly int texturesCount;
            public readonly int materialsCount;
            public readonly int lightsCount;
            public readonly int locatorsCount;
            public readonly int meshObjectsCount;
            public readonly int trianglesCount;
            public readonly int vertexBuffersCount;
            public readonly Vector3 boundsSize;
            public readonly Vector3 boundsCenter;
            public readonly float radius;
            public readonly int p19;
            public readonly int p20;
            public readonly int p21;

            public HeaderData(BinaryReader br)
            {
                version = new string(br.ReadChars(4));
                flags = br.ReadInt32();
                stringsDataLength = br.ReadInt32();
                stringsDataItemCount = br.ReadInt32();
                texturesCount = br.ReadInt32();
                materialsCount = br.ReadInt32();
                lightsCount = br.ReadInt32();
                locatorsCount = br.ReadInt32();
                meshObjectsCount = br.ReadInt32();
                trianglesCount = br.ReadInt32();
                vertexBuffersCount = br.ReadInt32();
                boundsSize = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                boundsCenter = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                radius = br.ReadSingle();
                p19 = 0;
                p20 = 0;
                p21 = 0;
                switch (version)
                {
                    case "10.1":
                        {
                            throw new ArgumentException($"Importing version '{version}' of the .gm file is not supported.");
                        }
                    case "20.1":
                        {
                            p19 = br.ReadInt32();
                            p20 = br.ReadInt32();
                            p21 = br.ReadInt32();
                            break;
                        }
                }
            }
        }

        public sealed class StringsData
        {
            public const char DATA_SEPARATOR = (char)0x00;

            public readonly string stringData;
            public readonly int[] startIndices;

            public readonly string[] strings;
            public readonly IDictionary<int, string> stringsByStartIndices;

            public StringsData(BinaryReader br, int dataLength, int itemsCount)
            {
                stringData = new string(br.ReadChars(dataLength));
                strings = stringData.Split(DATA_SEPARATOR, StringSplitOptions.RemoveEmptyEntries);
                if (strings.Length != itemsCount)
                {
                    throw new ArgumentException($"strings.Length != itemsCount: strings.Length={strings.Length}, itemsCount={itemsCount}");
                }
                startIndices = new int[itemsCount];
                stringsByStartIndices = new Dictionary<int, string>(itemsCount);
                for (int i = 0; i < itemsCount; ++i)
                {
                    int startIndex = br.ReadInt32();
                    startIndices[i] = startIndex;
                    if (!stringsByStartIndices.ContainsKey(startIndex))
                    {
                        stringsByStartIndices[startIndex] = strings[i];
                    }
                }
            }

            public string GetStringByStartIndex(int startIndex)
            {
                return stringsByStartIndices[startIndex];
            }
        }

        public sealed class TexturesData
        {
            public readonly int[] startIndices;
            public readonly string[] names;

            public TexturesData(BinaryReader br, int count, StringsData stringsData)
            {
                startIndices = new int[count];
                names = new string[count];
                for (int i = 0; i < count; ++i)
                {
                    int startIndex = br.ReadInt32();
                    startIndices[i] = startIndex;
                    names[i] = stringsData.GetStringByStartIndex(startIndex);
                }
            }
        }

        public sealed class MaterialsData
        {
            public readonly Material[] materials;

            public MaterialsData(BinaryReader br, string version, int count, StringsData stringsData)
            {
                materials = new Material[count];
                for (int i = 0; i < count; ++i)
                {
                    materials[i] = new Material(br, version, stringsData);
                }
            }

            public sealed class Material
            {
                public const int TEXTURES_COUNT = 4;

                public enum TextureTypes
                {
                    None = 0,
                    Main = 1,
                    Bump = 2
                }

                public readonly int groupStartIndex;
                public readonly int nameStartIndex;
                public readonly float p3;
                public readonly float p4;
                public readonly float p5;
                public readonly float p6;
                public readonly float diffuse;
                public readonly float specular;
                public readonly float gloss;
                public readonly float selfIllum;
                public readonly TextureTypes[] textureTypes;
                public readonly int[] textureIndices;

                public readonly string groupName;
                public readonly string name;

                public Material(BinaryReader br, string version, StringsData stringsData)
                {
                    groupStartIndex = br.ReadInt32();
                    nameStartIndex = br.ReadInt32();
                    p3 = 0;
                    p4 = 0;
                    p5 = 0;
                    p6 = 0;
                    switch (version)
                    {
                        case "20.1":
                            {
                                p3 = br.ReadSingle();
                                p4 = br.ReadSingle();
                                p5 = br.ReadSingle();
                                p6 = br.ReadSingle();
                                break;
                            }
                    }
                    diffuse = br.ReadSingle();
                    specular = br.ReadSingle();
                    gloss = br.ReadSingle();
                    selfIllum = br.ReadSingle();
                    textureTypes = new TextureTypes[TEXTURES_COUNT];
                    for (int i = 0; i < TEXTURES_COUNT; ++i)
                    {
                        textureTypes[i] = (TextureTypes)br.ReadInt32();
                    }
                    textureIndices = new int[TEXTURES_COUNT];
                    for (int i = 0; i < TEXTURES_COUNT; ++i)
                    {
                        textureIndices[i] = br.ReadInt32();
                    }

                    groupName = stringsData.GetStringByStartIndex(groupStartIndex);
                    name = stringsData.GetStringByStartIndex(nameStartIndex);
                }
            }
        }

        public sealed class LocatorsData
        {
            public readonly Locator[] locators;

            public LocatorsData(BinaryReader br, int count, StringsData stringsData)
            {
                locators = new Locator[count];
                for (int i = 0; i < count; ++i)
                {
                    locators[i] = new Locator(br, stringsData);
                }
            }

            public sealed class Locator
            {
                public const int BONES_COUNT = 4;

                public readonly int groupStartIndex;
                public readonly int nameStartIndex;
                public readonly int flags;
                public readonly Matrix4x4 matrix;
                public readonly int[] boneIndices;
                public readonly float[] boneWeights;

                public readonly string groupName;
                public readonly string name;

                public Locator(BinaryReader br, StringsData stringsData)
                {
                    groupStartIndex = br.ReadInt32();
                    nameStartIndex = br.ReadInt32();
                    flags = br.ReadInt32();
                    matrix = new Matrix4x4(
                        m11: br.ReadSingle(), m21: br.ReadSingle(), m31: br.ReadSingle(), m41: br.ReadSingle(),
                        m12: br.ReadSingle(), m22: br.ReadSingle(), m32: br.ReadSingle(), m42: br.ReadSingle(),
                        m13: br.ReadSingle(), m23: br.ReadSingle(), m33: br.ReadSingle(), m43: br.ReadSingle(),
                        m14: br.ReadSingle(), m24: br.ReadSingle(), m34: br.ReadSingle(), m44: br.ReadSingle()
                    );
                    boneIndices = new int[BONES_COUNT];
                    for (int i = 0; i < boneIndices.Length; ++i)
                    {
                        boneIndices[i] = br.ReadInt32();
                    }
                    boneWeights = new float[BONES_COUNT];
                    for (int i = 0; i < boneWeights.Length; ++i)
                    {
                        boneWeights[i] = br.ReadSingle();
                    }

                    groupName = stringsData.GetStringByStartIndex(groupStartIndex);
                    name = stringsData.GetStringByStartIndex(nameStartIndex);
                }
            }
        }

        public sealed class MeshObjectsData
        {
            public readonly MeshObject[] meshObjects;

            public MeshObjectsData(BinaryReader br, string version, int count, StringsData stringsData)
            {
                meshObjects = new MeshObject[count];
                for (int i = 0; i < count; ++i)
                {
                    meshObjects[i] = new MeshObject(br, version, stringsData);
                }
            }

            public sealed class MeshObject
            {
                public const int DATA_LENGTH = 12;

                public readonly int groupStartIndex;
                public readonly int nameStartIndex;
                public readonly int flags;
                public readonly Vector3 center;
                public readonly float radius;
                public readonly int vertexBufferIndex;
                public readonly int trianglesCount;
                public readonly int trianglesOffset;
                public readonly int verticesCount;
                public readonly int verticesOffset;
                public readonly int materialIndex;
                public readonly int[] data;
                public readonly int trianglesCountSum;

                public readonly string groupName;
                public readonly string name;

                public MeshObject(BinaryReader br, string version, StringsData stringsData)
                {
                    groupStartIndex = br.ReadInt32();
                    nameStartIndex = br.ReadInt32();
                    flags = br.ReadInt32();
                    center = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                    radius = br.ReadSingle();
                    vertexBufferIndex = br.ReadInt32();
                    trianglesCount = br.ReadInt32();
                    trianglesOffset = br.ReadInt32();
                    verticesCount = br.ReadInt32();
                    verticesOffset = br.ReadInt32();
                    materialIndex = br.ReadInt32();
                    data = new int[DATA_LENGTH];
                    for (int i = 0; i < data.Length; ++i)
                    {
                        data[i] = br.ReadInt32();
                    }
                    if (!string.Equals(version, "20.1"))
                    {
                        trianglesCountSum = br.ReadInt32();
                    }

                    groupName = stringsData.GetStringByStartIndex(groupStartIndex);
                    name = stringsData.GetStringByStartIndex(nameStartIndex);
                }
            }
        }

        public sealed class TrianglesData
        {
            public readonly Triangle[] triangles;

            public TrianglesData(BinaryReader br, int count)
            {
                triangles = new Triangle[count];
                for (int i = 0; i < count; ++i)
                {
                    triangles[i] = new Triangle(br);
                }
            }

            public readonly struct Triangle
            {
                public readonly ushort v1;
                public readonly ushort v2;
                public readonly ushort v3;

                public Triangle(BinaryReader br)
                {
                    v1 = br.ReadUInt16();
                    v2 = br.ReadUInt16();
                    v3 = br.ReadUInt16();
                }
            }
        }

        public sealed class VertexBuffersData
        {
            public readonly VertexBuffer[] vertexBuffers;

            public VertexBuffersData(BinaryReader br, int count)
            {
                vertexBuffers = new VertexBuffer[count];
                for (int i = 0; i < count; ++i)
                {
                    vertexBuffers[i] = new VertexBuffer(br);
                }
            }

            public sealed class VertexBuffer
            {
                public const int FLAG_UV2 = 1;
                public const int FLAG_ANIMATED = 4;

                public readonly int flags;
                public readonly int length;

                public readonly bool isAnimated;
                public readonly bool hasUv2;
                public readonly int bytesPerVertex;
                public readonly int verticesCount;

                public VertexBuffer(BinaryReader br)
                {
                    flags = br.ReadInt32();
                    length = br.ReadInt32();

                    isAnimated = (flags & FLAG_ANIMATED) == FLAG_ANIMATED;
                    hasUv2 = (flags & FLAG_UV2) == FLAG_UV2;
                    bytesPerVertex = 36;
                    if (isAnimated)
                    {
                        bytesPerVertex += 8;
                    }
                    if (hasUv2)
                    {
                        bytesPerVertex += 8;
                    }
                    verticesCount = length / bytesPerVertex;
                }
            }
        }

        public sealed class VerticesData
        {
            public readonly Dictionary<int, Vertex[]> verticesByBufferIndices;

            public VerticesData(BinaryReader br, VertexBuffersData.VertexBuffer[] vertexBuffers)
            {
                verticesByBufferIndices = new Dictionary<int, Vertex[]>(vertexBuffers.Length);
                int bufferIndex = 0;
                foreach (VertexBuffersData.VertexBuffer buffer in vertexBuffers)
                {
                    Vertex[] vertices = new Vertex[buffer.verticesCount];
                    for (int i = 0; i < buffer.verticesCount; ++i)
                    {
                        vertices[i] = new Vertex(br, buffer.isAnimated, buffer.hasUv2);
                    }
                    verticesByBufferIndices[bufferIndex] = vertices;
                    ++bufferIndex;
                }
            }

            public sealed class Vertex
            {
                public readonly Vector3 position;
                public readonly float? weight1;
                public readonly float? weight2;
                public readonly int? bones;
                public readonly int? bone1;
                public readonly int? bone2;
                public readonly Vector3 normal;
                public readonly byte colorR;
                public readonly byte colorG;
                public readonly byte colorB;
                public readonly byte colorA;
                public readonly Vector2 uv;
                public readonly Vector2? uv2;

                public Vertex(BinaryReader br, bool isAnimated, bool hasUv2)
                {
                    position = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                    if (isAnimated)
                    {
                        weight1 = br.ReadSingle();
                        weight2 = 1.0F - weight1;
                        bones = br.ReadInt32();
                        bone1 = bones & 0xFF;
                        bone2 = (bones >> 8) & 0xFF;
                    }
                    else
                    {
                        weight1 = null;
                        weight2 = null;
                        bones = null;
                        bone1 = null;
                        bone2 = null;
                    }
                    normal = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                    colorR = br.ReadByte();
                    colorG = br.ReadByte();
                    colorB = br.ReadByte();
                    colorA = br.ReadByte();
                    uv = new Vector2(br.ReadSingle(), br.ReadSingle());
                    if (hasUv2)
                    {
                        uv2 = new Vector2(br.ReadSingle(), br.ReadSingle());
                    }
                    else
                    {
                        uv2 = null;
                    }
                }
            }
        }
    }
}
