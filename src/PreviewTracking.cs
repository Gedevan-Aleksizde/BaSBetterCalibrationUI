using System;
using System.Collections.Generic;
using RootMotion.FinalIK;
using ThunderRoad;
using UnityEngine;

namespace BetterCalibrationUI
{
    // A continuous animation skeleton, independent of ragdoll mesh parenting and native player IK.
    internal sealed class PreviewTracking : IDisposable
    {
        internal readonly Transform Root;
        internal readonly Matrix4x4 WorldToLocalFrame;
        internal readonly Quaternion FrameRotation;
        internal readonly Dictionary<Transform, Transform> Bones = new Dictionary<Transform, Transform>();
        private readonly Dictionary<Ragdoll.Bone, Transform> skeleton = new Dictionary<Ragdoll.Bone, Transform>();
        private readonly Creature source;
        private readonly Player player;
        private readonly IKSolverVR solver = new IKSolverVR();
        private readonly Transform targetRoot;
        private readonly Transform head, leftHand, rightHand;
        private readonly Transform[] bodyProxies = new Transform[3];
        private readonly Quaternion[] bodyRotations = new Quaternion[3];
        private Vector3 headOffset, leftOffset, rightOffset;
        private Quaternion headRotation, leftRotation, rightRotation;

        internal PreviewTracking(Creature source, Player player, Transform origin)
        {
            this.source = source;
            this.player = player;
            Root = new GameObject("BCUI Animation Skeleton").transform;
            targetRoot = new GameObject("BCUI Physical IK Targets").transform;
            Root.SetPositionAndRotation(origin.position, origin.rotation);
            Root.localScale = source.transform.lossyScale;
            WorldToLocalFrame = Root.worldToLocalMatrix;
            FrameRotation = Root.rotation;
            try
            {
                foreach (var bone in source.ragdoll.bones) Clone(bone);
                StraightenArm(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, -Root.right);
                StraightenArm(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, Root.right);
                head = Target("Head"); leftHand = Target("Left Hand"); rightHand = Target("Right Hand");
                for (int i = 0; i < bodyProxies.Length; i++) bodyProxies[i] = Target("Body Target " + i);
                var references = new VRIK.References
                {
                    root = Root, pelvis = Bone(HumanBodyBones.Hips), spine = Bone(HumanBodyBones.Spine),
                    chest = Bone(HumanBodyBones.Chest), neck = Bone(HumanBodyBones.Neck), head = Bone(HumanBodyBones.Head),
                    leftShoulder = Bone(HumanBodyBones.LeftShoulder), leftUpperArm = Bone(HumanBodyBones.LeftUpperArm),
                    leftForearm = Bone(HumanBodyBones.LeftLowerArm), leftHand = Bone(HumanBodyBones.LeftHand),
                    rightShoulder = Bone(HumanBodyBones.RightShoulder), rightUpperArm = Bone(HumanBodyBones.RightUpperArm),
                    rightForearm = Bone(HumanBodyBones.RightLowerArm), rightHand = Bone(HumanBodyBones.RightHand),
                    leftThigh = Bone(HumanBodyBones.LeftUpperLeg), leftCalf = Bone(HumanBodyBones.LeftLowerLeg),
                    leftFoot = Bone(HumanBodyBones.LeftFoot), leftToes = Bone(HumanBodyBones.LeftToes),
                    rightThigh = Bone(HumanBodyBones.RightUpperLeg), rightCalf = Bone(HumanBodyBones.RightLowerLeg),
                    rightFoot = Bone(HumanBodyBones.RightFoot), rightToes = Bone(HumanBodyBones.RightToes)
                };
                if (!references.isFilled) throw new InvalidOperationException("The preview needs a complete humanoid animation skeleton.");
                Chain(references.leftUpperArm, references.leftForearm, references.leftHand);
                Chain(references.rightUpperArm, references.rightForearm, references.rightHand);
                Chain(references.leftThigh, references.leftCalf, references.leftFoot);
                Chain(references.rightThigh, references.rightCalf, references.rightFoot);
                solver.leftArm.wristToPalmAxis = source.handLeft.axisPalm;
                solver.leftArm.palmToThumbAxis = source.handLeft.axisThumb;
                solver.rightArm.wristToPalmAxis = source.handRight.axisPalm;
                solver.rightArm.palmToThumbAxis = source.handRight.axisThumb;
                solver.SetToReferences(references);
                solver.Initiate(Root);
                string message = null;
                if (!solver.IsValid(ref message) || !solver.initiated) throw new InvalidOperationException(message ?? "Preview IK initialization failed.");
                solver.spine.headTarget = head;
                solver.spine.positionWeight = solver.spine.rotationWeight = 1f;
                solver.leftArm.target = leftHand; solver.rightArm.target = rightHand;
                solver.leftArm.positionWeight = solver.leftArm.rotationWeight = 1f;
                solver.rightArm.positionWeight = solver.rightArm.rotationWeight = 1f;
                headOffset = source.centerEyes.InverseTransformPoint(source.ragdoll.headPart.transform.position) * source.transform.lossyScale.y;
                headRotation = Quaternion.Inverse(source.centerEyes.rotation) * source.ragdoll.headPart.transform.rotation;
                leftOffset = source.handLeft.grip.InverseTransformPoint(source.handLeft.transform.position) * source.transform.lossyScale.y;
                rightOffset = source.handRight.grip.InverseTransformPoint(source.handRight.transform.position) * source.transform.lossyScale.y;
                leftRotation = Quaternion.Inverse(source.handLeft.grip.rotation) * source.handLeft.transform.rotation;
                rightRotation = Quaternion.Inverse(source.handRight.grip.rotation) * source.handRight.transform.rotation;
            }
            catch { Dispose(); throw; }
        }

        private Transform Clone(Ragdoll.Bone bone)
        {
            if (skeleton.TryGetValue(bone, out var copy)) return copy;
            copy = new GameObject(bone.animation.name).transform;
            Transform parent = bone.parent != null ? Clone(bone.parent) : Root;
            copy.SetParent(parent, false);
            // Use one coherent cached reference pose. The current animation/physics pose may
            // already be deformed and its live parents need not match Bone.parent.
            copy.SetPositionAndRotation(Root.TransformPoint(bone.orgCreatureLocalPosition),
                Root.rotation * Quaternion.Inverse(bone.orgCreatureLocalRotation));
            skeleton.Add(bone, copy);
            if (bone.mesh) Bones[bone.mesh] = copy;
            return copy;
        }

        private Transform Bone(HumanBodyBones id)
        {
            Transform animation = source.animator.GetBoneTransform(id);
            if (!animation) return null;
            var bone = source.ragdoll.GetBone(animation);
            return bone != null ? Clone(bone) : null;
        }

        private static void Chain(Transform parent, Transform middle, Transform end)
        {
            if (!middle.IsChildOf(parent) || !end.IsChildOf(middle))
                throw new InvalidOperationException("Preview bones are not a continuous animation chain.");
        }

        private void StraightenArm(HumanBodyBones upper, HumanBodyBones lower, HumanBodyBones hand, Vector3 direction)
        {
            Transform arm = Bone(upper), elbow = Bone(lower), wrist = Bone(hand);
            if (!arm || !elbow || !wrist) throw new InvalidOperationException("Missing preview arm bones.");
            arm.rotation = Quaternion.FromToRotation(elbow.position - arm.position, direction) * arm.rotation;
            elbow.rotation = Quaternion.FromToRotation(wrist.position - elbow.position, direction) * elbow.rotation;
        }

        internal void Calibrate(Transform[] references)
        {
            // Reference display must not solve against three independent authored marker poses.
            // Align the intact reference skeleton as a whole, then leave its joint geometry intact.
            Root.position += references[0].position - Bone(HumanBodyBones.Hips).position;
            Set(head, Bone(HumanBodyBones.Head)); Set(leftHand, Bone(HumanBodyBones.LeftHand)); Set(rightHand, Bone(HumanBodyBones.RightHand));
            Transform[] anatomy = { Bone(HumanBodyBones.Hips), Bone(HumanBodyBones.LeftToes) ?? Bone(HumanBodyBones.LeftFoot), Bone(HumanBodyBones.RightToes) ?? Bone(HumanBodyBones.RightFoot) };
            for (int i = 0; i < 3; i++) bodyRotations[i] = Quaternion.Inverse(references[i].rotation) * anatomy[i].rotation;
            solver.StoreDefaultLocalState();
        }

        internal void Update(Transform[] assigned)
        {
            solver.FixTransforms();
            Transform physicalHead = player.head.transform;
            head.SetPositionAndRotation(physicalHead.position + physicalHead.rotation * headOffset, physicalHead.rotation * headRotation);
            Transform leftGrip = GripAnchor(player.handLeft), rightGrip = GripAnchor(player.handRight);
            leftHand.SetPositionAndRotation(leftGrip.position + leftGrip.rotation * leftOffset, leftGrip.rotation * leftRotation);
            rightHand.SetPositionAndRotation(rightGrip.position + rightGrip.rotation * rightOffset, rightGrip.rotation * rightRotation);
            ApplyTargets(assigned, false);
            solver.Update();
        }

        private void ApplyTargets(Transform[] targets, bool calibration)
        {
            for (int i = 0; i < 3; i++) if (targets[i]) bodyProxies[i].SetPositionAndRotation(targets[i].position, targets[i].rotation * bodyRotations[i]);
            solver.spine.pelvisTarget = targets[0] ? bodyProxies[0] : null;
            solver.spine.pelvisPositionWeight = solver.spine.pelvisRotationWeight = targets[0] ? 1f : 0f;
            solver.leftLeg.target = targets[1] ? bodyProxies[1] : null;
            solver.rightLeg.target = targets[2] ? bodyProxies[2] : null;
            solver.leftLeg.positionWeight = solver.leftLeg.rotationWeight = targets[1] ? 1f : 0f;
            solver.rightLeg.positionWeight = solver.rightLeg.rotationWeight = targets[2] ? 1f : 0f;
            solver.leftLeg.bendGoal = calibration ? null : targets[3]; solver.leftLeg.bendGoalWeight = solver.leftLeg.bendGoal ? 1f : 0f;
            solver.rightLeg.bendGoal = calibration ? null : targets[4]; solver.rightLeg.bendGoalWeight = solver.rightLeg.bendGoal ? 1f : 0f;
            solver.leftArm.bendGoal = calibration ? null : targets[5]; solver.leftArm.bendGoalWeight = solver.leftArm.bendGoal ? 1f : 0f;
            solver.rightArm.bendGoal = calibration ? null : targets[6]; solver.rightArm.bendGoalWeight = solver.rightArm.bendGoal ? 1f : 0f;
            solver.locomotion.weight = !calibration && (!targets[1] || !targets[2]) ? 1f : 0f;
        }

        private Transform Target(string name)
        {
            var target = new GameObject(name).transform;
            target.SetParent(targetRoot, false);
            return target;
        }

        private static void Set(Transform target, Transform value) => target.SetPositionAndRotation(value.position, value.rotation);

        private static Transform GripAnchor(PlayerHand hand)
        {
            // PlayerLink.Attach aligns the avatar's grip to this Rigidbody, not to the Offset root.
            if (hand.link && hand.link.controllerRigidbody) return hand.link.controllerRigidbody.transform;
            if (hand.grip) return hand.grip.transform;
            return hand.root;
        }

        internal void TracePose(string stage)
        {
            var message = new System.Text.StringBuilder("[BetterCalibrationUI] Pose snapshot: " + stage);
            message.Append("; scale=").Append(Root.lossyScale.ToString("F3"));
            foreach (HumanBodyBones id in new[] { HumanBodyBones.Hips, HumanBodyBones.Head, HumanBodyBones.LeftUpperArm,
                HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, HumanBodyBones.RightUpperArm,
                HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot })
            {
                Transform bone = Bone(id);
                if (bone) message.Append("; ").Append(id).Append('=').Append(WorldToLocalFrame.MultiplyPoint3x4(bone.position).ToString("F3"));
            }
            Debug.Log(message.ToString());
        }
        public void Dispose()
        {
            if (Root) { Root.gameObject.SetActive(false); UnityEngine.Object.Destroy(Root.gameObject); }
            if (targetRoot) UnityEngine.Object.Destroy(targetRoot.gameObject);
        }
    }
}
