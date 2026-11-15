using System;
using BetterCalibrationUI;

internal static class BoneTopologyTests
{
    private sealed class Bone { internal Bone Parent; }
    private static int checks;
    private static void Check(bool value) { if (!value) throw new Exception("Invalid skeleton binding"); checks++; }
    private static void Invalid(Action action)
    {
        try { action(); } catch (InvalidOperationException) { checks++; return; }
        throw new Exception("Invalid skeleton was accepted");
    }
    private static void Main()
    {
        var root = new Bone();
        var hip = new Bone { Parent = root };
        var upper = new Bone { Parent = hip };
        var forearm = new Bone { Parent = upper };
        var hand = new Bone { Parent = forearm };
        // Renderer bone order is not guaranteed to put parents before children.
        int[] parents = BoneTopology.Parents(new[] { hand, hip, forearm, upper }, b => b.Parent, root);
        Check(parents[0] == 2); Check(parents[1] == -1); Check(parents[2] == 3); Check(parents[3] == 1);
        Invalid(() => BoneTopology.Parents(new[] { hand, hip }, b => b.Parent, root));
        upper.Parent = hand;
        Invalid(() => BoneTopology.Parents(new[] { hand, forearm, upper }, b => b.Parent, root));
        hip.Parent = hip;
        Invalid(() => BoneTopology.Parents(new[] { hip }, b => b.Parent, root));
        Console.WriteLine("Render skeleton binding: " + checks + " regression checks passed.");
    }
}
