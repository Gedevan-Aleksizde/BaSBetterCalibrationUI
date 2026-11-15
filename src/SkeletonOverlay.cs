using System;
using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;
using UnityEngine.Rendering;

namespace BetterCalibrationUI
{
    internal sealed class SkeletonOverlay : IDisposable
    {
        private readonly GameObject root;
        private readonly List<Segment> segments = new List<Segment>();
        private Material material;
        private Shader loadedShader;
        private bool disposed;
        private sealed class Segment
        {
            internal Transform from, to;
            internal LineRenderer line;
        }

        internal SkeletonOverlay(VisualRig rig, Creature source)
        {
            root = new GameObject("BCUI Bone Overlay");
            root.transform.SetParent(rig.Root, false);
            Chain(rig, source, HumanBodyBones.Hips, HumanBodyBones.Spine, HumanBodyBones.Chest,
                HumanBodyBones.UpperChest, HumanBodyBones.Neck, HumanBodyBones.Head);
            Chain(rig, source, HumanBodyBones.Chest, HumanBodyBones.LeftShoulder, HumanBodyBones.LeftUpperArm,
                HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand);
            Chain(rig, source, HumanBodyBones.Chest, HumanBodyBones.RightShoulder, HumanBodyBones.RightUpperArm,
                HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand);
            Chain(rig, source, HumanBodyBones.Hips, HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg,
                HumanBodyBones.LeftFoot, HumanBodyBones.LeftToes);
            Chain(rig, source, HumanBodyBones.Hips, HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg,
                HumanBodyBones.RightFoot, HumanBodyBones.RightToes);
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            if (shader && shader.isSupported) SetMaterial(shader);
            else
            {
                try { Catalog.LoadAssetAsync<Shader>("Shader.Custom.AlwaysVisibleTexture", OnShaderLoaded, "BCUI bone overlay"); }
                catch (Exception error) { Debug.LogWarning("[BetterCalibrationUI] Bone overlay shader could not be loaded: " + error.Message); }
            }
        }

        private void Chain(VisualRig rig, Creature source, params HumanBodyBones[] bones)
        {
            Transform previous = null;
            foreach (HumanBodyBones id in bones)
            {
                Transform current = rig.HumanBone(source, id);
                if (!current) continue;
                if (previous && previous != current)
                {
                    var go = new GameObject(previous.name + " -> " + current.name);
                    go.transform.SetParent(root.transform, false);
                    var line = go.AddComponent<LineRenderer>();
                    line.enabled = false;
                    line.useWorldSpace = true;
                    line.positionCount = 2;
                    line.startWidth = line.endWidth = 0.006f;
                    line.startColor = line.endColor = Color.cyan;
                    line.numCapVertices = 3;
                    line.shadowCastingMode = ShadowCastingMode.Off;
                    line.receiveShadows = false;
                    segments.Add(new Segment { from=previous, to=current, line=line });
                }
                previous = current;
            }
        }

        private void OnShaderLoaded(Shader shader)
        {
            if (disposed) { if (shader) Catalog.ReleaseAsset(shader); return; }
            if (!shader || !shader.isSupported)
            {
                if (shader) Catalog.ReleaseAsset(shader);
                Debug.LogWarning("[BetterCalibrationUI] No overlay shader is available; bone lines are disabled.");
                return;
            }
            loadedShader = shader;
            SetMaterial(shader);
        }

        private void SetMaterial(Shader shader)
        {
            material = new Material(shader) { name="BCUI Bone Lines", renderQueue=4000 };
            material.SetInt("_ZTest", (int)CompareFunction.Always);
            material.SetInt("_ZWrite", 0);
            material.SetInt("_Cull", (int)CullMode.Off);
            if (material.HasProperty("_Color")) material.SetColor("_Color", Color.cyan);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.cyan);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", Texture2D.whiteTexture);
            foreach (Segment segment in segments) segment.line.sharedMaterial = material;
        }

        internal void Update()
        {
            if (disposed) return;
            foreach (Segment segment in segments)
            {
                bool visible = BCUIChanger.showBones && material && segment.from && segment.to;
                segment.line.enabled = visible;
                if (!visible) continue;
                segment.line.SetPosition(0, segment.from.position);
                segment.line.SetPosition(1, segment.to.position);
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (root) { root.SetActive(false); UnityEngine.Object.Destroy(root); }
            if (material) UnityEngine.Object.Destroy(material);
            if (loadedShader) Catalog.ReleaseAsset(loadedShader);
        }
    }
}
