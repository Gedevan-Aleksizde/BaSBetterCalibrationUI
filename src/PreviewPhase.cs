namespace BetterCalibrationUI
{
    internal sealed class PreviewPhase
    {
        internal bool Visible { get; private set; }
        internal bool Tracking { get; private set; }
        private bool wasCalibrating;
        internal bool Update(bool calibrating)
        {
            bool start = calibrating && !wasCalibrating;
            if (start) { Visible = true; Tracking = false; }
            if (wasCalibrating && !calibrating && Visible) Tracking = true;
            wasCalibrating = calibrating;
            return start;
        }
        internal void Reset() { wasCalibrating = Visible = Tracking = false; }
    }
}
