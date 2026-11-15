using System.Collections.Generic;
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

        internal VisualRig(Transform source, string name, bool render)
        {
            this.source = source;
            Root = new GameObject(name).transform;
            Bones[source] = Root;
            if (!render) return;
            var lowerLods = new HashSet<Renderer>();
            foreach (LODGroup group in source.GetComponentsInChildren<LODGroup>(true))
            {
                LOD[] lods = group.GetLODs();
                if (lods.Length == 0) continue;
                var highest = new HashSet<Renderer>(lods[0].renderers);
                for (int i = 1; i < lods.Length; i++)
                    foreach (Renderer renderer in lods[i].renderers)
                        if (renderer && !highest.Contains(renderer)) lowerLods.Add(renderer);
            }
            foreach (SkinnedMeshRenderer original in source.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
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

        internal void CopyPoseFrom(IReadOnlyDictionary<Transform, Transform> other)
        {
            // Include intermediate transforms before copying, even if no renderer referenced them.
            foreach (Transform original in other.Keys) Map(original);
            foreach (var pair in Bones)
            {
                if (pair.Key == source || !other.TryGetValue(pair.Key, out var pose)) continue;
                pair.Value.localPosition = pose.localPosition;
                pair.Value.localRotation = pose.localRotation;
                pair.Value.localScale = pose.localScale;
            }
        }

        internal void Destroy() { if (Root) Object.Destroy(Root.gameObject); }
    }
}

