using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using BetterCalibrationUI;
using UnityEngine;

internal static class PreviewGeometryTests
{
    static int checks;
    static void Main(string[] args)
    {
        AppDomain.CurrentDomain.AssemblyResolve += (sender, e) =>
            Assembly.LoadFrom(Path.Combine(args[0], new AssemblyName(e.Name).Name + ".dll"));
        Run();
    }

    static void Near(float actual, float expected)
    {
        if (Math.Abs(actual - expected) > 0.0001f)
            throw new Exception("Expected " + expected + ", got " + actual);
        checks++;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Run()
    {
        // A translated/yawed menu scene uses its playerStart anchor, including its floor elevation.
        Vector3 front = PreviewGeometry.MenuPosition(new Vector3(12, .3f, -8), new Vector3(1, 0, 0), 1.8f);
        Near(front.x, 14.52f); Near(front.z, -8); Near(front.y, .3f);
        // Another scene facing the opposite direction must not assume world +Z.
        Vector3 opposite = PreviewGeometry.MenuPosition(new Vector3(12, .3f, -8), new Vector3(-1, 0, 0), 1.8f);
        Near(opposite.x, 9.48f); Near(opposite.y, .3f); Near(opposite.z, -8);
        // A short avatar retains the minimum preview distance along menu forward.
        Vector3 shortAvatar = PreviewGeometry.MenuPosition(new Vector3(2, 0, 5), new Vector3(0, 0, 1), 1.2f);
        Near(shortAvatar.x, 2); Near(shortAvatar.y, 0); Near(shortAvatar.z, 7.25f);
        // The pivot stays at the intended position; centering belongs inside the rotating display.
        Vector3 hips = new Vector3(.3f, 1.1f, -.2f);
        Vector3 centered = hips + PreviewGeometry.CenterOffset(hips);
        var yaw90 = new Matrix4x4(new Vector4(0,0,-1,0), new Vector4(0,1,0,0), new Vector4(1,0,0,0), new Vector4(4,0,6,1));
        Vector3 turned = yaw90.MultiplyPoint3x4(centered);
        Near(turned.x, 4); Near(turned.y, 1.1f); Near(turned.z, 6);
        Vector3 arm = PreviewGeometry.ReferenceSegment(new Vector3(-.3f, 0, 0), .45f);
        Near(arm.x, -.45f); Near(arm.y, 0); Near(arm.z, 0);
        // Different render/animation parents: transfer the world point through stable frames.
        var physical = new Matrix4x4(new Vector4(.5f,0,0,0), new Vector4(0,.5f,0,0), new Vector4(0,0,.5f,0), new Vector4(-5,0,0,1));
        var display = new Matrix4x4(new Vector4(-3,0,0,0), new Vector4(0,3,0,0), new Vector4(0,0,3,0), new Vector4(4,0,6,1));
        Vector3 pose = PreviewGeometry.PosePoint(physical, display, new Vector3(12,4,6));
        Near(pose.x, 1); Near(pose.y, 6); Near(pose.z, 15);
        Console.WriteLine("Preview geometry: " + checks + " checks passed.");
    }
}
