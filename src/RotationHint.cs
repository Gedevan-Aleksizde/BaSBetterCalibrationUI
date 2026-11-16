using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.UI;

namespace BetterCalibrationUI
{
    // Copy only the two native images. Native rotation buttons, callbacks and constraints stay untouched.
    internal sealed class RotationHint : IDisposable
    {
        private readonly GameObject root;
        private readonly Vector3 offset;
        private readonly Quaternion orientation;

        internal static RotationHint Create(GameObject nativeUI)
        {
            if (!nativeUI) return null;
            RectTransform template = nativeUI.GetComponent<RectTransform>();
            Transform circle = nativeUI.transform.Find("Circle");
            Transform stick = circle ? circle.Find("ThumbStick") : null;
            if (!template || !circle || !stick || !circle.GetComponent<Image>() || !stick.GetComponent<Image>())
            {
                Debug.LogWarning("[BetterCalibrationUI] Native circle/thumbstick images are missing; rotation remains available.");
                return null;
            }
            try { return new RotationHint(template, circle.GetComponent<Image>(), stick.GetComponent<Image>()); }
            catch (Exception error)
            {
                Debug.LogWarning("[BetterCalibrationUI] Rotation hint creation failed: " + error.Message);
                return null;
            }
        }

        private RotationHint(RectTransform template, Image circle, Image stick)
        {
            root = new GameObject("BCUI Rotation Hint", typeof(RectTransform), typeof(Canvas));
            root.SetActive(false);
            try
            {
                root.layer = template.gameObject.layer;
                var rect = (RectTransform)root.transform;
                rect.sizeDelta = template.rect.size;
                rect.pivot = template.pivot;
                rect.localScale = template.lossyScale;
                orientation = template.rotation;
                PositionConstraint constraint = template.GetComponent<PositionConstraint>();
                offset = constraint ? constraint.translationOffset : Vector3.up * .02f;
                Canvas canvas = root.GetComponent<Canvas>();
                Canvas originalCanvas = template.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                if (originalCanvas)
                {
                    canvas.worldCamera = originalCanvas.worldCamera;
                    canvas.sortingLayerID = originalCanvas.sortingLayerID;
                    canvas.sortingOrder = originalCanvas.sortingOrder;
                    canvas.additionalShaderChannels = originalCanvas.additionalShaderChannels;
                    canvas.referencePixelsPerUnit = originalCanvas.referencePixelsPerUnit;
                }
                Image circleCopy = CopyImage(circle, rect);
                CopyImage(stick, circleCopy.transform);
            }
            catch { Dispose(); throw; }
        }

        private static Image CopyImage(Image source, Transform parent)
        {
            var target = new GameObject(source.name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            target.layer = source.gameObject.layer;
            var rect = (RectTransform)target.transform;
            rect.SetParent(parent, false);
            RectTransform original = source.rectTransform;
            rect.anchorMin = original.anchorMin;
            rect.anchorMax = original.anchorMax;
            rect.pivot = original.pivot;
            rect.sizeDelta = original.sizeDelta;
            rect.anchoredPosition3D = original.anchoredPosition3D;
            rect.localRotation = original.localRotation;
            rect.localScale = original.localScale;
            Image image = target.GetComponent<Image>();
            image.sprite = source.sprite;
            image.overrideSprite = source.overrideSprite;
            image.material = source.material;
            image.color = source.color;
            image.type = source.type;
            image.preserveAspect = source.preserveAspect;
            image.fillCenter = source.fillCenter;
            image.fillMethod = source.fillMethod;
            image.fillAmount = source.fillAmount;
            image.fillClockwise = source.fillClockwise;
            image.fillOrigin = source.fillOrigin;
            image.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
            image.raycastTarget = false;
            return image;
        }

        internal void Update(Transform pivot, bool canRotate)
        {
            bool visible = canRotate && pivot;
            root.SetActive(visible);
            if (visible) root.transform.SetPositionAndRotation(pivot.position + offset, orientation);
        }

        public void Dispose()
        {
            if (!root) return;
            root.SetActive(false);
            UnityEngine.Object.Destroy(root);
        }
    }
}
