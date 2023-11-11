using System.Runtime.CompilerServices;
using Unity.Mathematics;

using static Unity.Mathematics.math;

namespace Ugol.UnityMathematicsExtensions
{
    public static class UnityMathematicsExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion RotateTowards(this quaternion from, quaternion to, float maxAngleDelta)
        {
            float num = Angle(from, to);
            return num < float.Epsilon ? to : slerp(from, to, min(1f, maxAngleDelta / num));
        }
     
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Angle(this quaternion q1, quaternion q2)
        {
            var dot = math.dot(q1, q2);
            return !(dot > 0.999998986721039) ? (float) (acos(min(abs(dot), 1f)) * 2.0) : 0.0f;
        }
    }
}
