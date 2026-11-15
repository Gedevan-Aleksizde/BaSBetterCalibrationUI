using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using BetterCalibrationUI;
using UnityEngine;

internal static class PreviewGeometryTests
{
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
        Console.WriteLine("Preview geometry: 9 checks passed.");
    }
}
