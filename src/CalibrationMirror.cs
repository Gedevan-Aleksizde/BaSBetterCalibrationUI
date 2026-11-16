using System;
using System.Collections.Generic;
using System.Reflection;
using ThunderRoad;
using UnityEngine;

namespace BetterCalibrationUI
{
    [DefaultExecutionOrder(10000)]
    public sealed class CalibrationMirror : MonoBehaviour
    {
        private static readonly FieldInfo Markers = typeof(TrackersPage).GetField("trackerMarkers", BindingFlags.Instance | BindingFlags.NonPublic);
        private readonly PreviewPhase phase = new PreviewPhase();
        private readonly Dictionary<InputBase.Tracker, VisualRig> markerCopies = new Dictionary<InputBase.Tracker, VisualRig>();
        private NativeCalibration native;
        private bool calibrating, gripReleased;
        private Vector3 displayOffset;
        private TrackersPage page;
        private Creature source;
        private Player player;
        private VisualRig display;
        private PreviewTracking tracking;
        private SkeletonOverlay bones;
        private RotationHint rotationHint;
        private Transform displayPivot;
        private PlayerControl.Hand stick;
        private bool inputDetached;
        private bool failed;

        private void Awake() { page = GetComponent<TrackersPage>(); native = new NativeCalibration(page); }

        private void OnEnable()
        {
            phase.Reset();
            failed = false;
            Debug.Log("[BetterCalibrationUI] Tracker page active; binding calibration controls (" + GetType().Assembly.GetName().Version + ").");
            if (page && page.calibrateButton)
            {
                page.calibrateButton.onPointerClick.RemoveListener(CalibrationClicked);
                page.calibrateButton.onPointerClick.AddListener(CalibrationClicked);
                page.clearCalibrationButton.onPointerClick.AddListener(ResetClicked);
            }
            if (native.IsCalibrating) CalibrationClicked();
            native.RefreshTrackerStatus(true);
        }

        private void CalibrationClicked()
        {
            if (!BCUIChanger.VRChatInterface) return;
            failed = false;
            Debug.Log("[BetterCalibrationUI] Calibration button received.");
            try { native.Begin(); calibrating = true; gripReleased = false; phase.Update(true); CreatePreview(); }
            catch (Exception error) { Fail("Preview creation failed", error); }
        }

        private void LateUpdate()
        {
            if (!page || !page.characterSelection || !Player.local) return;
            try
            {
                native.RefreshTrackerStatus();
                if (failed) return;
                if (calibrating)
                {
                    bool grip = PlayerControl.handLeft.gripPressed || PlayerControl.handRight.gripPressed;
                    if (!grip) gripReleased = true;
                    if (grip && gripReleased) { native.Confirm(); calibrating = false; }
                }
                bool wasTracking = phase.Tracking;
                if (phase.Update(calibrating)) CreatePreview();
                if (display == null) return;
                if (!source || source != page.characterSelection.GetCharacterCreature()) { Clear(); phase.Reset(); return; }
                if (phase.Tracking)
                {
                    tracking.Update(native.AssignedTargets());
                    if (!wasTracking) { tracking.TracePose("first tracking solve"); Debug.Log("[BetterCalibrationUI] Safe assignment confirmed; independent animation-skeleton preview tracking enabled."); }
                    CopyPose();
                    if (!wasTracking) display.TraceTransfer(tracking.Bones, tracking.WorldToLocalFrame, "first tracking solve");
                    float axis = stick != null ? stick.JoystickAxis.x : 0f;
                    if (Mathf.Abs(axis) > 0.65f)
                        displayPivot.Rotate(0f, axis * page.characterSelection.characterRotationSpeed * Time.unscaledDeltaTime, 0f, Space.World);
                }
                UpdateMarkers();
                bones?.Update();
                rotationHint?.Update(displayPivot, phase.Tracking && stick != null);
            }
            catch (Exception error) { Fail("Mirror preview failed", error); }
        }

        private void Fail(string message, Exception error)
        {
            Debug.LogError("[BetterCalibrationUI] " + message + ": " + error);
            if (calibrating) native.Finish();
            calibrating = false;
            Clear();
            phase.Reset();
            failed = true;
        }

        private void CreatePreview()
        {
            Clear();
            source = page.characterSelection.GetCharacterCreature();
            player = Player.local;
            if (!source || !page.rootMarker || !page.trackerModel || !page.characterSelection.playerStart)
                throw new InvalidOperationException("Calibration preview references are missing.");
            display = new VisualRig(source.transform, "BCUI Mirror Avatar", true, excludeEquipment: true);
            tracking = new PreviewTracking(source, player, page.rootMarker);
            tracking.Calibrate(native.ReferenceTargets());
            tracking.TracePose("calibration reference");
            Transform menuStart = page.characterSelection.playerStart;
            Vector3 forward = PreviewGeometry.HorizontalForward(menuStart.forward);
            Vector3 position = PreviewGeometry.MenuPosition(menuStart.position, menuStart.forward, source.GetHeight());
            displayPivot = new GameObject("BCUI Display Pivot").transform;
            displayPivot.SetPositionAndRotation(position, Quaternion.LookRotation(-forward, Vector3.up));
            Vector3 scale = source.transform.lossyScale;
            displayPivot.localScale = new Vector3(-scale.x, scale.y, scale.z);
            display.Root.SetParent(displayPivot, false);
            CopyPose();
            Transform hips = display.HumanBone(source, HumanBodyBones.Hips);
            displayOffset = PreviewGeometry.CenterOffset(display.Root.InverseTransformPoint(hips.position));
            CopyPose();
            display.TraceTransfer(tracking.Bones, tracking.WorldToLocalFrame, "calibration reference");
            bones = new SkeletonOverlay(display, source);
            bones.Update();
            rotationHint = RotationHint.Create(page.characterSelection.characterMovementUI);
            stick = PlayerControl.handLeft;
            if (stick != null)
            {
                stick.OnJoystickMoveEvent -= page.characterSelection.MoveCreature;
                inputDetached = true;
            }
            UpdateMarkers();
            Debug.Log("[BetterCalibrationUI] Mirror preview created with a continuous animation skeleton; native Animator unchanged.");
        }

        private void CopyPose()
        {
            if (display == null || tracking == null) return;
            display.Root.localPosition = displayOffset;
            display.ApplyWorldPose(tracking.Bones, tracking.Root, tracking.WorldToLocalFrame, tracking.FrameRotation);
        }

        private void UpdateMarkers()
        {
            foreach (VisualRig copy in markerCopies.Values) copy.Root.gameObject.SetActive(false);
            var markers = Markers.GetValue(page) as List<TrackersPage.TrackerMarker>;
            if (markers == null) return;
            foreach (var marker in markers)
            {
                if (!NativeCalibration.IsBodyDevice(marker.inputTracker)) continue;
                if (!PlayerControl.input.GetTrackerPositionAndRotation(marker.inputTracker, out var pos, out var rot)) continue;
                if (!markerCopies.TryGetValue(marker.inputTracker, out var copy))
                {
                    copy = new VisualRig(page.trackerModel, "BCUI Tracker Marker", true);
                    copy.Root.SetParent(display.Root, false);
                    Vector3 scale = tracking.Root.lossyScale;
                    Vector3 model = page.trackerModel.lossyScale;
                    copy.Root.localScale = new Vector3(model.x / scale.x, model.y / scale.y, model.z / scale.z);
                    markerCopies.Add(marker.inputTracker, copy);
                }
                copy.Root.localPosition = tracking.WorldToLocalFrame.MultiplyPoint3x4(player.transform.TransformPoint(pos));
                copy.Root.localRotation = Quaternion.Inverse(tracking.FrameRotation) * player.transform.rotation * rot;
                copy.Root.gameObject.SetActive(true);
            }
        }

        private void Clear()
        {
            rotationHint?.Dispose();
            rotationHint = null;
            bones?.Dispose();
            bones = null;
            foreach (VisualRig marker in markerCopies.Values) marker.Destroy();
            markerCopies.Clear();
            display?.Destroy();
            tracking?.Dispose();
            display = null;
            tracking = null;
            if (displayPivot) Destroy(displayPivot.gameObject);
            displayPivot = null;
            if (inputDetached && stick != null && page && page.characterSelection && page.characterSelection.isActiveAndEnabled && page.characterSelection.IsCharacterVisible)
            {
                stick.OnJoystickMoveEvent -= page.characterSelection.MoveCreature;
                stick.OnJoystickMoveEvent += page.characterSelection.MoveCreature;
            }
            inputDetached = false;
            stick = null;
            source = null;
            player = null;
            displayOffset = Vector3.zero;
        }

        private void ResetClicked()
        {
            calibrating = false;
            PlayerControl.input.trackers = new InputBase.Tracker[7];
            native.FilterDevices();
            native.RefreshTrackerStatus(true);
            Clear();
            phase.Reset();
        }

        private void OnDisable()
        {
            if (page && page.calibrateButton) page.calibrateButton.onPointerClick.RemoveListener(CalibrationClicked);
            if (page && page.clearCalibrationButton) page.clearCalibrationButton.onPointerClick.RemoveListener(ResetClicked);
            if (calibrating && page)
            {
                if (BCUIChanger.IsRunning && !BCUIChanger.VRChatInterface && page.isActiveAndEnabled)
                    native.BeginVanilla();
                else native.Finish();
            }
            calibrating = false;
            Clear();
            phase.Reset();
            if (page && page.isActiveAndEnabled) page.RefreshTrackerState();
        }

        private void OnDestroy() => Clear();
    }
}
