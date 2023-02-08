using System.Numerics;

namespace AoP.Editor
{
    public sealed class SystemNumericsMapper
    {
        
        public UnityEngine.Vector3 ToUnity(Vector3 vector3)
        {
            return new UnityEngine.Vector3(vector3.X, vector3.Y, vector3.Z);
        }

        public UnityEngine.Quaternion ToUnity(Quaternion quaternion)
        {
            return new UnityEngine.Quaternion(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);
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
