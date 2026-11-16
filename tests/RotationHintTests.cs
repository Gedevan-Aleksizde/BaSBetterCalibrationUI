using System;
using System.Collections.Generic;
using System.Linq;
using BetterCalibrationUI;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.UI;

internal static class RotationHintTests
{
    private static int checks;
    private static void Check(bool value) { if (!value) throw new Exception("Rotation hint regression"); checks++; }
    private static void Main()
    {
        var native = new GameObject("Native Hint", typeof(RectTransform), typeof(Canvas), typeof(PositionConstraint));
        native.SetActive(false);
        native.GetComponent<PositionConstraint>().translationOffset = new Vector3(.01f, .02f, -.12f);
        native.transform.rotation = new Quaternion(1);
        var circle = new GameObject("Circle", typeof(RectTransform), typeof(Image));
        circle.transform.SetParent(native.transform, false);
        circle.GetComponent<Image>().sprite = new object();
        var stick = new GameObject("ThumbStick", typeof(RectTransform), typeof(Image));
        stick.transform.SetParent(circle.transform, false);
        stick.GetComponent<Image>().sprite = new object();
        var button = new GameObject("Rotate Right Button", typeof(RectTransform), typeof(Image));
        button.transform.SetParent(native.transform, false);
        Check(RotationHint.Create(null) == null);
        Check(RotationHint.Create(new GameObject("Missing UI")) == null);
        var hint = RotationHint.Create(native);
        var display = GameObject.All.Single(g => g.name == "BCUI Rotation Hint");
        Check(!display.activeSelf);
        Check(display.transform.Find("Circle/ThumbStick") != null);
        Check(display.transform.Find("Rotate Right Button") == null);
        var copiedCircle = display.transform.Find("Circle").GetComponent<Image>();
        var copiedStick = display.transform.Find("Circle/ThumbStick").GetComponent<Image>();
        Check(ReferenceEquals(copiedCircle.sprite, circle.GetComponent<Image>().sprite));
        Check(ReferenceEquals(copiedStick.sprite, stick.GetComponent<Image>().sprite));
        Check(!copiedCircle.raycastTarget && !copiedStick.raycastTarget);
        var pivot = new GameObject("Avatar Pivot").transform;
        pivot.position = new Vector3(3, 0, 5);
        hint.Update(pivot, false);
        Check(!display.activeSelf);
        hint.Update(pivot, true);
        Check(display.activeSelf);
        Check(Math.Abs(display.transform.position.x - 3.01f) < .0001f);
        Check(Math.Abs(display.transform.position.y - .02f) < .0001f);
        Check(Math.Abs(display.transform.position.z - 4.88f) < .0001f);
        pivot.rotation = new Quaternion(2);
        hint.Update(pivot, true);
        Check(display.transform.rotation.Value == 1);
        hint.Update(null, true);
        Check(!display.activeSelf);
        hint.Update(pivot, true);
        hint.Update(pivot, false);
        Check(!display.activeSelf);
        hint.Dispose();
        Check(display.Destroyed && !display.activeSelf);
        Check(!native.Destroyed && !native.activeSelf);
        Check(native.GetComponent<PositionConstraint>().enabled);
        Console.WriteLine("Native rotation hint: " + checks + " lifecycle checks passed.");
    }
}

// In-memory scene/UI doubles; tests compile the production RotationHint implementation.
namespace UnityEngine
{
    public class Object
    {
        public bool Destroyed;
        public static implicit operator bool(Object value) => value != null && !value.Destroyed;
        public static void Destroy(Object value) { value.Destroyed = true; }
    }
    public class Component : Object
    {
        public GameObject gameObject;
        public string name => gameObject.name;
        public Transform transform => gameObject.transform;
        public T GetComponent<T>() where T : Component => gameObject.GetComponent<T>();
    }
    public struct Vector2 { public float x, y; }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 up => new Vector3(0, 1, 0);
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x+b.x, a.y+b.y, a.z+b.z);
        public static Vector3 operator *(Vector3 a, float b) => new Vector3(a.x*b, a.y*b, a.z*b);
    }
    public struct Quaternion { public int Value; public Quaternion(int value) { Value = value; } }
    public class Transform : Component
    {
        public Vector3 position, localScale = new Vector3(1,1,1);
        public Quaternion rotation, localRotation;
        public Vector3 lossyScale => localScale;
        private Transform parent;
        private readonly List<Transform> children = new List<Transform>();
        public void SetParent(Transform target, bool preserve)
        {
            parent?.children.Remove(this); parent = target; target.children.Add(this);
        }
        public Transform Find(string path)
        {
            Transform current = this;
            foreach (string name in path.Split('/'))
            {
                current = current.children.FirstOrDefault(c => c.name == name);
                if (current == null) break;
            }
            return current;
        }
        public void SetPositionAndRotation(Vector3 position, Quaternion rotation) { this.position=position; this.rotation=rotation; }
    }
    public class RectTransform : Transform
    {
        public Vector2 anchorMin, anchorMax, pivot, sizeDelta;
        public Vector3 anchoredPosition3D;
        public Rect rect => new Rect { size=sizeDelta };
    }
    public struct Rect { public Vector2 size; }
    public enum RenderMode { WorldSpace }
    public class Canvas : Component
    {
        public RenderMode renderMode;
        public object worldCamera;
        public int sortingLayerID, sortingOrder, additionalShaderChannels;
        public float referencePixelsPerUnit;
    }
    public class CanvasRenderer : Component { }
    public sealed class GameObject : Object
    {
        public static readonly List<GameObject> All = new List<GameObject>();
        public string name;
        public int layer;
        public bool activeSelf = true;
        public Transform transform;
        private readonly Dictionary<Type, Component> components = new Dictionary<Type, Component>();
        public GameObject(string name, params Type[] types)
        {
            this.name = name;
            var transformType = types.Contains(typeof(RectTransform)) ? typeof(RectTransform) : typeof(Transform);
            transform = (Transform)Activator.CreateInstance(transformType);
            transform.gameObject = this;
            components.Add(transformType, transform);
            foreach (Type type in types)
            {
                if (type == transformType) continue;
                var component = (Component)Activator.CreateInstance(type);
                component.gameObject = this;
                components.Add(type, component);
            }
            All.Add(this);
        }
        public T GetComponent<T>() where T : Component => components.Values.OfType<T>().FirstOrDefault();
        public void SetActive(bool active) { activeSelf = active; }
    }
    public static class Debug { public static void LogWarning(string message) { } }
}
namespace UnityEngine.Animations
{
    public class PositionConstraint : Component { public Vector3 translationOffset; public bool enabled = true; }
}
namespace UnityEngine.UI
{
    public class Image : Component
    {
        public object sprite, overrideSprite, material, color;
        public int type, fillMethod, fillOrigin;
        public bool preserveAspect, fillCenter, fillClockwise, raycastTarget = true;
        public float fillAmount, pixelsPerUnitMultiplier;
        public RectTransform rectTransform => (RectTransform)transform;
    }
}
