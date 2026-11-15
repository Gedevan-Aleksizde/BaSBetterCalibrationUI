using System;
using System.Collections.Generic;
using System.Reflection;
using FbtMore;
using ThunderRoad;
using UnityEngine;

namespace BetterCalibrationUI
{
    [DefaultExecutionOrder(10000)]
    public sealed class CalibrationMirror : MonoBehaviour
    {
        private static readonly FieldInfo Calibrating = typeof(TrackersPage).GetField("isCalibrating", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo Markers = typeof(TrackersPage).GetField("trackerMarkers", BindingFlags.Instance | BindingFlags.NonPublic);
        private readonly PreviewPhase phase = new PreviewPhase();
        private readonly Dictionary<InputBase.Tracker, VisualRig> markerCopies = new Dictionary<InputBase.Tracker, VisualRig>();
        private readonly BodyTargets bodyTargets = new BodyTargets();
        private TrackersPage page;
        private Creature source;
        private Player player;
        private VisualRig display;
        private TrackingSession tracking;
        private SkeletonOverlay bones;
        private Transform displayPivot;
        private PlayerControl.Hand stick;
        private bool inputDetached;
        private bool failed;

        private void Awake() => page = GetComponent<TrackersPage>();

        private void OnEnable()
        {
            phase.Reset();
            failed = false;
            if (page && page.calibrateButton)
            {
                page.calibrateButton.onPointerClick.RemoveListener(CalibrationClicked);
                page.calibrateButton.onPointerClick.AddListener(CalibrationClicked);
            }
        }

        private void CalibrationClicked()
        {
            failed = false;
            try { phase.Update(true); CreatePreview(); }
            catch (Exception error) { Fail("Preview creation failed", error); }
        }

        private void LateUpdate()
        {
            if (failed || !page || !page.characterSelection || !Player.local) return;
            try
            {
                bool calibrating = page.textTrackerCalibration && page.textTrackerCalibration.enabled && (bool)Calibrating.GetValue(page);
                bool wasTracking = phase.Tracking;
                if (phase.Update(calibrating)) CreatePreview();
                if (display == null) return;
                if (!source || source != page.characterSelection.GetCharacterCreature()) { Clear(); phase.Reset(); return; }
                if (phase.Tracking)
                {
                    ReadBodyTargets(false);
                    if (!wasTracking)
                    {
                        tracking.BeginTracking(bodyTargets);
                        Debug.Log("[BetterCalibrationUI] Tracker assignment complete; FBT More tracking and left-stick rotation enabled.");
                    }
                    else tracking.UpdateTracking(bodyTargets);
                    CopyPose();
                    float axis = stick != null ? stick.JoystickAxis.x : 0f;
                    if (Mathf.Abs(axis) > 0.65f)
                        displayPivot.Rotate(0f, axis * page.characterSelection.characterRotationSpeed * Time.unscaledDeltaTime, 0f, Space.World);
                }
                UpdateMarkers();
                bones?.Update();
            }
            catch (Exception error) { Fail("Mirror preview failed", error); }
        }

        private void Fail(string message, Exception error)
        {
            Debug.LogError("[BetterCalibrationUI] " + message + ": " + error);
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
            display = new VisualRig(source.transform, "BCUI Mirror Avatar", true);
            tracking = TrackingRuntime.CreateSession(source, player, page.rootMarker);
            ReadBodyTargets(true);
            tracking.SetCalibrationPose(bodyTargets);
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
            displayPivot.position += Vector3.ProjectOnPlane(position - hips.position, Vector3.up);
            bones = new SkeletonOverlay(display, source);
            bones.Update();
            stick = PlayerControl.handLeft;
            if (stick != null)
            {
                stick.OnJoystickMoveEvent -= page.characterSelection.MoveCreature;
                inputDetached = true;
            }
            UpdateMarkers();
            Debug.Log("[BetterCalibrationUI] Mirror calibration preview created using FBT More.");
        }

        private void ReadBodyTargets(bool calibration)
        {
            bool female = source.data.gender == CreatureData.Gender.Female;
            Transform leftFoot = female ? page.leftFootFemaleMarker : page.leftFootMaleMarker;
            Transform rightFoot = female ? page.rightFootFemaleMarker : page.rightFootMaleMarker;
            bodyTargets.Pelvis = calibration ? page.waistMarker : Assigned(page.waistMarker);
            bodyTargets.LeftFoot = calibration ? leftFoot : Assigned(leftFoot);
            bodyTargets.RightFoot = calibration ? rightFoot : Assigned(rightFoot);
            bodyTargets.LeftKnee = calibration ? null : Assigned(page.leftKneeMarker);
            bodyTargets.RightKnee = calibration ? null : Assigned(page.rightKneeMarker);
            bodyTargets.LeftElbow = calibration ? null : Assigned(page.leftElbowMarker);
            bodyTargets.RightElbow = calibration ? null : Assigned(page.rightElbowMarker);
        }

        private Transform Assigned(Transform marker) => marker && marker.gameObject.activeInHierarchy && marker.parent != page.rootMarker ? marker : null;

        private void CopyPose()
        {
            if (display == null || tracking == null) return;
            display.CopyPoseFrom(tracking.BoneMappings);
            display.Root.localPosition = tracking.WorldToLocalFrame.MultiplyPoint3x4(tracking.Root.position);
            display.Root.localRotation = Quaternion.Inverse(tracking.FrameRotation) * tracking.Root.rotation;
        }

        private void UpdateMarkers()
        {
            foreach (VisualRig copy in markerCopies.Values) copy.Root.gameObject.SetActive(false);
            var markers = Markers.GetValue(page) as List<TrackersPage.TrackerMarker>;
            if (markers == null) return;
            foreach (var marker in markers)
            {
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
                copy.Root.localPosition = tracking.Root.InverseTransformPoint(player.transform.TransformPoint(pos));
                copy.Root.localRotation = Quaternion.Inverse(tracking.Root.rotation) * player.transform.rotation * rot;
                copy.Root.gameObject.SetActive(true);
            }
        }

        private void Clear()
        {
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
            if (inputDetached && stick != null && page && page.characterSelection && page.characterSelection.isActiveAndEnabled)
            {
                stick.OnJoystickMoveEvent -= page.characterSelection.MoveCreature;
                stick.OnJoystickMoveEvent += page.characterSelection.MoveCreature;
            }
            inputDetached = false;
            stick = null;
            source = null;
            player = null;
            bodyTargets.Pelvis = bodyTargets.LeftFoot = bodyTargets.RightFoot = null;
            bodyTargets.LeftKnee = bodyTargets.RightKnee = bodyTargets.LeftElbow = bodyTargets.RightElbow = null;
        }

        private void OnDisable()
        {
            if (page && page.calibrateButton) page.calibrateButton.onPointerClick.RemoveListener(CalibrationClicked);
            Clear();
            phase.Reset();
        }

        private void OnDestroy() => Clear();
    }
}
