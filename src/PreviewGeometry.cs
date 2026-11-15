using System;
using UnityEngine;

namespace BetterCalibrationUI
{
    internal static class PreviewGeometry
    {
        internal static Vector3 MenuPosition(Vector3 menuOrigin, Vector3 menuForward, float height)
        {
            Vector3 forward = HorizontalForward(menuForward);
            return menuOrigin + forward * Math.Max(2.25f, height * 1.4f);
        }

        internal static Vector3 HorizontalForward(Vector3 gaze)
        {
            float length = (float)Math.Sqrt(gaze.x * gaze.x + gaze.z * gaze.z);
            return length < 0.0001f ? Vector3.forward : new Vector3(gaze.x / length, 0f, gaze.z / length);
        }

    }
}
