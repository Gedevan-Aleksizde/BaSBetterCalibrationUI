using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using UnityEngine;

namespace BetterCalibrationUI
{
    // Copies only transforms and render components. Never clones Creature, physics, scripts or cameras.
    internal sealed class VisualRig
    {
        internal readonly Transform Root;
        internal readonly Dictionary<Transform, Transform> Bones = new Dictionary<Transform, Transform>();
        private readonly Transform source;
        private Transform solvedRoot;
        private readonly Dictionary<Transform, Transform> drivenBones = new Dictionary<Transform, Transform>();

        internal VisualRig(Transform source, string name, bool render, bool excludeEquipment = false)
        {
            this.source = source;
            Root = new GameObject(name).transform;
            Bones[source] = Root;
            if (!render) return;
            var lowerLods = new HashSet<Renderer>();
            foreach (LODGroup group in source.GetComponentsInChildren<LODGroup>(true))
            {
                if (excludeEquipment && EquipmentVisualFilter.IsExcluded(group.transform, source)) continue;
                LOD[] lods = group.GetLODs();
                if (lods.Length == 0) continue;
                var highest = new HashSet<Renderer>(lods[0].renderers);
                for (int i = 1; i < lods.Length; i++)
                    foreach (Renderer renderer in lods[i].renderers)
                        if (renderer && !highest.Contains(renderer)) lowerLods.Add(renderer);
            }
            foreach (SkinnedMeshRenderer original in source.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (excludeEquipment && EquipmentVisualFilter.IsExcluded(original.transform, source)) continue;
                if (!original.sharedMesh || lowerLods.Contains(original)) continue;
                var copy = Map(original.transform).gameObject.AddComponent<SkinnedMeshRenderer>();
                copy.sharedMesh = original.sharedMesh;
                copy.sharedMaterials = original.sharedMaterials;
                var bones = new Transform[original.bones.Length];
                for (int i = 0; i < bones.Length; i++) bones[i] = original.bones[i] ? Map(original.bones[i]) : null;
                copy.bones = bones;
                copy.rootBone = original.rootBone ? Map(original.rootBone) : Root;
                copy.localBounds = original.localBounds;
                copy.quality = original.quality;
                copy.updateWhenOffscreen = true;
                for (int i = 0; i < original.sharedMesh.blendShapeCount; i++) copy.SetBlendShapeWeight(i, original.GetBlendShapeWeight(i));
            }
            foreach (MeshRenderer original in source.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (excludeEquipment && EquipmentVisualFilter.IsExcluded(original.transform, source)) continue;
                var filter = original.GetComponent<MeshFilter>();
                if (!filter || !filter.sharedMesh || lowerLods.Contains(original)) continue;
                Transform target = Map(original.transform);
                target.gameObject.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                target.gameObject.AddComponent<MeshRenderer>().sharedMaterials = original.sharedMaterials;
            }
        }

        internal Transform Map(Transform original)
        {
            if (!original || !original.IsChildOf(source) && original != source) return null;
            if (Bones.TryGetValue(original, out var target)) return target;
            target = new GameObject(original.name).transform;
            target.SetParent(Map(original.parent), false);
            target.localPosition = original.localPosition;
            target.localRotation = original.localRotation;
            target.localScale = original.localScale;
            Bones.Add(original, target);
            return target;
        }

        internal Transform HumanBone(Creature creature, HumanBodyBones id)
        {
            Transform animation = creature.animator.GetBoneTransform(id);
            if (!animation) return null;
            Ragdoll.Bone bone = creature.ragdoll.GetBone(animation);
            return bone != null && bone.mesh ? Map(bone.mesh) : null;
        }

        internal void ApplyWorldPose(IReadOnlyDictionary<Transform, Transform> other, Transform trackingRoot, Matrix4x4 frame, Quaternion frameRotation)
        {
            if (!solvedRoot)
            {
                solvedRoot = new GameObject("BCUI Render Skeleton").transform;
                solvedRoot.SetParent(Root, false);
                foreach (var pair in other) drivenBones.Add(pair.Value, Map(pair.Key));
                var poses = drivenBones.Keys.ToList();
                int[] parents = BoneTopology.Parents(poses, pose => pose.parent, trackingRoot);
                // Detach first so a cached ragdoll parent can never introduce a reparenting cycle.
                foreach (Transform target in drivenBones.Values) target.SetParent(solvedRoot, true);
                for (int i = 0; i < poses.Count; i++)
                {
                    Transform parent = parents[i] == -1 ? solvedRoot : drivenBones[poses[parents[i]]];
                    drivenBones[poses[i]].SetParent(parent, false);
                }
                Debug.Log("[BetterCalibrationUI] Render skeleton rebound to " + drivenBones.Count + " continuous animation bones.");
            }
            solvedRoot.localPosition = frame.MultiplyPoint3x4(trackingRoot.position);
            solvedRoot.localRotation = Quaternion.Inverse(frameRotation) * trackingRoot.rotation;
            foreach (var pair in drivenBones)
            {
                pair.Value.localPosition = pair.Key.localPosition;
                pair.Value.localRotation = pair.Key.localRotation;
                pair.Value.localScale = pair.Key.localScale;
            }
        }

        internal void TraceTransfer(IReadOnlyDictionary<Transform, Transform> pose, Matrix4x4 frame, string stage)
        {
            float maximum = 0f;
            foreach (var pair in pose)
            {
                Vector3 expected = frame.MultiplyPoint3x4(pair.Value.position);
                Vector3 actual = Root.InverseTransformPoint(Bones[pair.Key].position);
                maximum = Mathf.Max(maximum, Vector3.Distance(expected, actual));
            }
            Debug.Log("[BetterCalibrationUI] Render transfer " + stage + ": max frame-space bone error=" + maximum.ToString("F6") + ".");
        }

        internal void Destroy() { if (Root) Object.Destroy(Root.gameObject); }
    }
}

