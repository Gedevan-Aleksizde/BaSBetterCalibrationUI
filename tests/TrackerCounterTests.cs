using System;
using BetterCalibrationUI;

internal static class TrackerCounterTests
{
    private static int checks;
    private static void Check(bool value) { if (!value) throw new Exception("Tracker total regression"); checks++; }
    private static TrackerConnection Tracker(string serial, bool connected = true) => new TrackerConnection(serial, TrackerDeviceKind.Tracker, connected);
    private static void Main()
    {
        var feetAndWrists = new[] { Tracker("left-foot"), Tracker("right-foot"), Tracker("left-wrist-used-by-another-mod"), Tracker("right-wrist-used-by-another-mod") };
        Check(TrackerCounter.Connected(feetAndWrists) == 4);
        // Counting does not use the 7 native body slots or require any body-role assignment.
        Check(TrackerCounter.Connected(new[] { feetAndWrists[2], feetAndWrists[3] }) == 2);
        Check(TrackerCounter.Connected(new[] { feetAndWrists[0], feetAndWrists[0], feetAndWrists[2] }) == 2);
        Check(TrackerCounter.Connected(new[] { Tracker("left-wrist", false), Tracker("right-wrist") }) == 1);
        Check(TrackerCounter.Connected(new[] {
            new TrackerConnection("hmd", TrackerDeviceKind.Headset, true),
            new TrackerConnection("controller", TrackerDeviceKind.Controller, true),
            new TrackerConnection("station", TrackerDeviceKind.TrackingReference, true),
            new TrackerConnection("unknown", TrackerDeviceKind.Unknown, true),
            Tracker("foot") }) == 1);
        Check(TrackerCounter.Connected(Array.Empty<TrackerConnection>()) == 0);
        Check(TrackerCounter.Connected(new[] { Tracker(null), Tracker("") }) == 0);
        // Extra connected trackers are counted even when only feet are assigned in BaS.
        Check(TrackerCounter.Connected(new[] { feetAndWrists[0], feetAndWrists[1], feetAndWrists[2], feetAndWrists[3], Tracker("additional-left"), Tracker("additional-right") }) == 6);
        Console.WriteLine("Tracker connection totals: " + checks + " regression checks passed.");
    }
}
