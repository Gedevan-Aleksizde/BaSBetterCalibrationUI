using System;
using BetterCalibrationUI;
internal static class PreviewPhaseTests
{
    static void Check(bool value) { if(!value) throw new Exception("Invalid preview transition"); }
    static void Main()
    {
        var phase=new PreviewPhase();
        Check(!phase.Update(false) && !phase.Visible && !phase.Tracking);
        Check(phase.Update(true) && phase.Visible && !phase.Tracking);
        Check(!phase.Update(true) && !phase.Tracking);
        Check(!phase.Update(false) && phase.Tracking && phase.Visible);
        Check(!phase.Update(false) && phase.Tracking);
        Check(phase.Update(true) && !phase.Tracking && phase.Visible);
        phase.Reset(); Check(!phase.Visible && !phase.Tracking);
        Check(!phase.Update(false) && !phase.Visible);
        Check(phase.Update(true)); Check(!phase.Update(false) && phase.Tracking);
        Console.WriteLine("Preview lifecycle: 10 checks passed.");
    }
}
