using System;
using System.Collections.Generic;

namespace BetterCalibrationUI
{
    internal enum TrackerDeviceKind { Unknown, Tracker, Headset, Controller, TrackingReference }

    internal readonly struct TrackerConnection
    {
        internal readonly string Identity;
        internal readonly TrackerDeviceKind Kind;
        internal readonly bool Connected;
        internal TrackerConnection(string identity, TrackerDeviceKind kind, bool connected)
        { Identity = identity; Kind = kind; Connected = connected; }
    }

    internal static class TrackerCounter
    {
        internal static int Connected(IEnumerable<TrackerConnection> devices)
        {
            var identities = new HashSet<string>(StringComparer.Ordinal);
            foreach (var device in devices)
                if (device.Connected && device.Kind == TrackerDeviceKind.Tracker && !string.IsNullOrEmpty(device.Identity))
                    identities.Add(device.Identity);
            return identities.Count;
        }
    }
}
