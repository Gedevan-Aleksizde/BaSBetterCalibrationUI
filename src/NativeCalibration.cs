using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using ThunderRoad;
using UnityEngine;
using UnityEngine.XR;
using Valve.VR;

namespace BetterCalibrationUI
{
    // The native proximity classifier cannot distinguish a wrist from a waist tracker.
    // Keep its UI/save format, but confirm only explicit supported SteamVR body roles.
    internal sealed class NativeCalibration
    {
        private static readonly FieldInfo Calibrating = Field("isCalibrating");
        private static readonly FieldInfo MarkerList = Field("trackerMarkers");
        private static readonly string[] Names = { "waistTrackerMarker", "leftFootTrackerMarker", "rightFootTrackerMarker", "leftKneeTrackerMarker", "rightKneeTrackerMarker", "leftElbowTrackerMarker", "rightElbowTrackerMarker" };
        private readonly TrackersPage page;
        private readonly Dictionary<string, TrackerRole> roles = new Dictionary<string, TrackerRole>();

        internal NativeCalibration(TrackersPage page) { this.page = page; }
        private static FieldInfo Field(string name) => typeof(TrackersPage).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        internal List<TrackersPage.TrackerMarker> Markers => (List<TrackersPage.TrackerMarker>)MarkerList.GetValue(page);

        internal static bool IsBodyDevice(InputBase.Tracker tracker)
        {
            if (tracker?.inputObject is InputDevice device)
                return device.isValid && (device.characteristics & (InputDeviceCharacteristics.TrackingReference | InputDeviceCharacteristics.HeadMounted | InputDeviceCharacteristics.Controller)) == 0;
            return false;
        }

        internal void FilterDevices()
        {
            bool removed = false;
            for (int i = Markers.Count - 1; i >= 0; i--)
            {
                var marker = Markers[i];
                if (IsBodyDevice(marker.inputTracker)) continue;
                if (marker.transform) UnityEngine.Object.Destroy(marker.transform.gameObject);
                Markers.RemoveAt(i);
                removed = true;
            }
            if (removed) page.RefreshTrackerList();
        }

        internal void Begin()
        {
            page.StartCalibration();
            // Stop TrackersPage.Update from running its unvalidated grip/proximity assignment.
            Calibrating.SetValue(page, false);
            foreach (string name in Names) Field(name).SetValue(page, null);
            FilterDevices();
            ReadRoles();
        }

        private void ReadRoles()
        {
            roles.Clear();
            try
            {
                CVRSystem system = OpenVR.System;
                if (system == null) return; // Fail closed; never guess an unknown device's body part.
                for (uint index = 0; index < OpenVR.k_unMaxTrackedDeviceCount; index++)
                {
                    if (!system.IsTrackedDeviceConnected(index) || system.GetTrackedDeviceClass(index) != ETrackedDeviceClass.GenericTracker) continue;
                    string serial = Property(system, index, ETrackedDeviceProperty.Prop_SerialNumber_String);
                    string type = Property(system, index, ETrackedDeviceProperty.Prop_ControllerType_String);
                    string profile = Property(system, index, ETrackedDeviceProperty.Prop_InputProfilePath_String);
                    if (!string.IsNullOrEmpty(serial)) roles[serial] = TrackerPolicy.Resolve(type, profile);
                }
            }
            catch (Exception error) { Debug.LogWarning("[BetterCalibrationUI] Tracker roles unavailable; no positional role guessing: " + error.Message); }
        }

        private static string Property(CVRSystem system, uint index, ETrackedDeviceProperty property)
        {
            var buffer = new StringBuilder(256);
            ETrackedPropertyError error = ETrackedPropertyError.TrackedProp_Success;
            system.GetStringTrackedDeviceProperty(index, property, buffer, (uint)buffer.Capacity, ref error);
            return error == ETrackedPropertyError.TrackedProp_Success ? buffer.ToString() : "";
        }

        internal Transform[] ReferenceTargets()
        {
            bool female = page.characterSelection.GetCharacterCreature().data.gender == CreatureData.Gender.Female;
            return new[] { page.waistMarker, female ? page.leftFootFemaleMarker : page.leftFootMaleMarker,
                female ? page.rightFootFemaleMarker : page.rightFootMaleMarker, page.leftKneeMarker, page.rightKneeMarker,
                page.leftElbowMarker, page.rightElbowMarker };
        }

        internal void Confirm()
        {
            // Native Clear also clears stale saved identifiers/offsets in memory. Save remains the user's action.
            page.ClearTrackerAssignements();
            FilterDevices();
            ReadRoles();
            Transform[] targets = ReferenceTargets();
            PlayerControl.input.trackers = new InputBase.Tracker[7];
            for (int slot = 0; slot < Names.Length; slot++)
            {
                TrackersPage.TrackerMarker best = null;
                float nearest = float.PositiveInfinity;
                foreach (var marker in Markers)
                {
                    if (!roles.TryGetValue(marker.inputTracker.identifier, out TrackerRole role) || (int)role != slot) continue;
                    if (!PlayerControl.input.GetTrackerPositionAndRotation(marker.inputTracker, out var position, out var rotation)) continue;
                    marker.transform.SetLocalPositionAndRotation(position, rotation);
                    float distance = Vector3.Distance(targets[slot].position, marker.transform.position);
                    if (TrackerPolicy.CanAssign(role, distance) && distance < nearest) { nearest = distance; best = marker; }
                }
                if (best == null) continue;
                targets[slot].SetParent(best.transform, true);
                targets[slot].gameObject.SetActive(true);
                Field(Names[slot]).SetValue(page, best);
                PlayerControl.input.trackers[slot] = best.inputTracker;
            }
            Finish();
            page.RefreshTrackerList();
            page.RefreshTrackerState();
            page.EnableClearCalibration(true);
            Debug.Log("[BetterCalibrationUI] Confirmed explicit SteamVR body roles; wrists/unknown devices are not assigned to waist or legs.");
        }

        internal Transform[] AssignedTargets()
        {
            Transform[] targets = ReferenceTargets();
            for (int slot = 0; slot < Names.Length; slot++)
            {
                var marker = Field(Names[slot]).GetValue(page) as TrackersPage.TrackerMarker;
                if (marker == null || !roles.TryGetValue(marker.inputTracker.identifier, out var role) || (int)role != slot)
                    targets[slot] = null;
            }
            return targets;
        }

        internal void Finish()
        {
            Calibrating.SetValue(page, false);
            page.saveButton.IsInteractable = true;
            page.calibrateButton.IsInteractable = true;
            page.textTrackerPresentation.enabled = true;
            page.textTrackerWarning.enabled = true;
            page.textTrackerCalibration.enabled = false;
            page.videoPlayer.gameObject.SetActive(true);
            page.ShowStaticMarkers(false);
        }
    }
}
