using System.Numerics;

namespace AoP.Editor
{
    public sealed class SystemNumericsMapper
    {
        public UnityEngine.Vector2 ToUnity(Vector2 vector2)
        {
            return new UnityEngine.Vector2(vector2.X, vector2.Y);
        }

        public UnityEngine.Vector3 ToUnity(Vector3 vector3)
        {
            return new UnityEngine.Vector3(vector3.X, vector3.Y, vector3.Z);
        }

        public UnityEngine.Vector3[] ToUnity(Vector3[] vectors3)
        {
            UnityEngine.Vector3[] result = new UnityEngine.Vector3[vectors3.Length];
            for (int i = 0; i < result.Length; ++i)
            {
                result[i] = ToUnity(vectors3[i]);
            }
            return result;
        }

        public UnityEngine.Quaternion ToUnity(Quaternion quaternion)
        {
            return new UnityEngine.Quaternion(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);
        }

        public UnityEngine.Quaternion[,] ToUnity(Quaternion[,] quaternions)
        {
            int rows = quaternions.GetLength(0);
            int columns = quaternions.GetLength(1);
            UnityEngine.Quaternion[,] result = new UnityEngine.Quaternion[rows, columns];
            for (int i = 0; i < rows; ++i)
            {
                for (int j = 0; j < columns; ++j)
                {
                    result[i, j] = ToUnity(quaternions[i, j]);
                }
            }
            return result;
        }

        public UnityEngine.Matrix4x4 ToUnity(Matrix4x4 matrix4x4)
        {
            return new UnityEngine.Matrix4x4()
            {
                m00 = matrix4x4.M11,
                m01 = matrix4x4.M12,
                m02 = matrix4x4.M13,
                m03 = matrix4x4.M14,
                m10 = matrix4x4.M21,
                m11 = matrix4x4.M22,
                m12 = matrix4x4.M23,
                m13 = matrix4x4.M24,
                m20 = matrix4x4.M31,
                m21 = matrix4x4.M32,
                m22 = matrix4x4.M33,
                m23 = matrix4x4.M34,
                m30 = matrix4x4.M41,
                m31 = matrix4x4.M42,
                m32 = matrix4x4.M43,
                m33 = matrix4x4.M44
            };
        }
    }
}
