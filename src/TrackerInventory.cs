using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.XR;
using Valve.VR;

namespace BetterCalibrationUI
{
    internal static class TrackerInventory
    {
        internal static int ConnectedCount()
        {
            var trackers = new List<TrackerConnection>();
            try
            {
                CVRSystem system = OpenVR.System;
                if (system != null)
                {
                    // Enumerate the runtime itself. Other mods reading a wrist tracker do not remove it.
                    for (uint index = 0; index < OpenVR.k_unMaxTrackedDeviceCount; index++)
                    {
                        if (!system.IsTrackedDeviceConnected(index) || system.GetTrackedDeviceClass(index) != ETrackedDeviceClass.GenericTracker) continue;
                        var serial = new StringBuilder(256);
                        ETrackedPropertyError error = ETrackedPropertyError.TrackedProp_Success;
                        system.GetStringTrackedDeviceProperty(index, ETrackedDeviceProperty.Prop_SerialNumber_String, serial, (uint)serial.Capacity, ref error);
                        string identity = error == ETrackedPropertyError.TrackedProp_Success && serial.Length > 0 ? serial.ToString() : "openvr:" + index;
                        trackers.Add(new TrackerConnection(identity, TrackerDeviceKind.Tracker, true));
                    }
                    return TrackerCounter.Connected(trackers);
                }
            }
            catch (Exception) { /* XR fallback when OpenVR enumeration is unavailable. */ }

            trackers.Clear();
            var devices = new List<InputDevice>();
            InputDevices.GetDevices(devices);
            for (int index = 0; index < devices.Count; index++)
            {
                var device = devices[index];
                InputDeviceCharacteristics flags = device.characteristics;
                if ((flags & InputDeviceCharacteristics.TrackedDevice) == 0) continue;
                TrackerDeviceKind kind = (flags & InputDeviceCharacteristics.HeadMounted) != 0 ? TrackerDeviceKind.Headset :
                    (flags & InputDeviceCharacteristics.TrackingReference) != 0 ? TrackerDeviceKind.TrackingReference :
                    (flags & InputDeviceCharacteristics.Controller) != 0 ? TrackerDeviceKind.Controller : TrackerDeviceKind.Tracker;
                string serial = device.serialNumber;
                trackers.Add(new TrackerConnection(string.IsNullOrEmpty(serial) ? "xr:" + index : serial, kind, device.isValid));
            }
            return TrackerCounter.Connected(trackers);
        }
    }
}
