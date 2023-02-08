using System.IO;
using System.Numerics;
using System.Text;

namespace AoP.Editor
{
    public sealed class FileAn
    {

        public readonly string name;
        public readonly string nameWithoutExtension;
        public readonly string path;
        public readonly HeaderData headerData;
        public readonly BonesData bonesData;
        public readonly FramesData framesData;

        public FileAn(string path)
        {
            name = Path.GetFileName(path);
            nameWithoutExtension = Path.GetFileNameWithoutExtension(path);
            this.path = path;
            using FileStream fs = File.OpenRead(path);
            using BinaryReader br = new(fs, Encoding.ASCII);
            headerData = new HeaderData(br);
            bonesData = new BonesData(br, headerData.bonesCount);
            framesData = new FramesData(br, headerData.framesCount, headerData.bonesCount);
        }

        public sealed class HeaderData
        {
            public readonly int framesCount;
            public readonly int bonesCount;
            public readonly float fps;

            public HeaderData(BinaryReader br)
            {
                framesCount = br.ReadInt32();
                bonesCount = br.ReadInt32();
                fps = br.ReadSingle();
            }
        }

        public sealed class BonesData
        {
            public readonly int[] parentIndices;
            public readonly Vector3[] startPositions;

            public BonesData(BinaryReader br, int count)
            {
                parentIndices = new int[count];
                for (int i = 0; i < count; ++i)
                {
                    parentIndices[i] = br.ReadInt32();
                }
                startPositions = new Vector3[count];
                for (int i = 0; i < count; ++i)
                {
                    startPositions[i] = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                }
            }
        }

        public sealed class FramesData
        {
            public readonly Vector3[] rootBonePositionByFrames;
            public readonly Quaternion[,] boneRotationByFrames;

            public FramesData(BinaryReader br, int framesCount, int jointsCount)
            {
                rootBonePositionByFrames = new Vector3[framesCount];
                for (int i = 0; i < framesCount; ++i)
                {
                    rootBonePositionByFrames[i] = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                }
                boneRotationByFrames = new Quaternion[jointsCount, framesCount];
                for (int i = 0; i < jointsCount; ++i)
                {
                    for (int j = 0; j < framesCount; ++j)
                    {
                        boneRotationByFrames[i, j] = new Quaternion(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                    }
                }
            }
        }
    }
}
