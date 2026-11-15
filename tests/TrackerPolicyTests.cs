using System;
using BetterCalibrationUI;

internal static class TrackerPolicyTests
{
    private static int checks;
    private static void Check(bool value) { if (!value) throw new Exception("Unsafe tracker assignment policy"); checks++; }

    private static void Main()
    {
        string[] parts = { "waist", "left_foot", "right_foot", "left_knee", "right_knee", "left_elbow", "right_elbow" };
        for (int slot = 0; slot < parts.Length; slot++)
            Check((int)TrackerPolicy.Parse("vive_tracker_" + parts[slot]) == slot);

        // Actual observed wrist/handed devices must never be selected for the waist or knees.
        foreach (string type in new[] { "vive_tracker_left_wrist", "vive_tracker_right_wrist", "vive_tracker_handed", "vive_tracker", "lighthouse", "", null })
        {
            Check(TrackerPolicy.Parse(type) == TrackerRole.None);
            Check(!TrackerPolicy.CanAssign(TrackerPolicy.Parse(type), 0f));
        }
        Check(TrackerPolicy.CanAssign(TrackerRole.Waist, .1f));
        Check(TrackerPolicy.CanAssign(TrackerRole.LeftElbow, .5f));
        Check(!TrackerPolicy.CanAssign(TrackerRole.RightKnee, 1.483f));
        Check(!TrackerPolicy.CanAssign(TrackerRole.Waist, float.NaN));
        Check(!TrackerPolicy.CanAssign(TrackerRole.Waist, float.PositiveInfinity));
        Check(!TrackerPolicy.CanAssign(TrackerRole.LeftFoot, -.1f));
        Check(!TrackerPolicy.CanAssign((TrackerRole)99, 0f));
        Check(TrackerPolicy.Resolve("vive_tracker", "{htc}/input/tracker/vive_tracker_waist_profile.json") == TrackerRole.Waist);
        Check(TrackerPolicy.Resolve("vive_tracker", "{tundra}/input/tracker/tundra_tracker_left_foot_profile.json") == TrackerRole.LeftFoot);
        Check(TrackerPolicy.Resolve("vive_tracker_left_wrist", "{htc}/input/tracker/vive_tracker_waist_profile.json") == TrackerRole.None);
        Check(TrackerPolicy.Resolve("vive_tracker", "{htc}/input/tracker/vive_tracker_right_wrist_profile.json") == TrackerRole.None);
        Check(TrackerPolicy.Resolve("vive_tracker_handed", "") == TrackerRole.None);
        Console.WriteLine("Tracker assignment: " + checks + " regression checks passed.");
    }
}
