using System;
using System.Collections.Generic;
using BetterCalibrationUI;
using ThunderRoad;
using UnityEngine;

internal static class EquipmentVisualFilterTests
{
    private static int checks;
    private static Transform Child(Transform parent, params object[] components)
    {
        var node = new Transform { parent = parent };
        node.Components.AddRange(components);
        return node;
    }
    private static void Check(bool value) { if (!value) throw new Exception("Avatar equipment filtering regression"); checks++; }
    private static void Main()
    {
        var avatar = new Transform();
        var hips = Child(avatar);
        var body = Child(hips);
        var hair = Child(Child(avatar));
        var clothing = Child(hips, new WardrobeModel());
        Check(!EquipmentVisualFilter.IsExcluded(body, avatar));
        Check(!EquipmentVisualFilter.IsExcluded(hair, avatar));
        Check(!EquipmentVisualFilter.IsExcluded(clothing, avatar));
        var slot = Child(hips, new Holder());
        Check(EquipmentVisualFilter.IsExcluded(slot, avatar));
        Check(EquipmentVisualFilter.IsExcluded(Child(Child(slot)), avatar));
        var holsteredWeapon = Child(slot, new Item());
        Check(EquipmentVisualFilter.IsExcluded(Child(holsteredWeapon), avatar));
        var directlyAttachedItem = Child(hips, new Item());
        Check(EquipmentVisualFilter.IsExcluded(Child(directlyAttachedItem), avatar));
        var customSlot = Child(hips, new CustomHolder());
        Check(EquipmentVisualFilter.IsExcluded(Child(customSlot), avatar));
        slot.Active = false;
        Check(EquipmentVisualFilter.IsExcluded(Child(slot), avatar));
        // Do not classify the avatar by an external parent's components.
        avatar.parent = Child(null, new Item());
        Check(!EquipmentVisualFilter.IsExcluded(body, avatar));
        Check(!EquipmentVisualFilter.IsExcluded(avatar, avatar));
        Console.WriteLine("Avatar equipment exclusion: " + checks + " regression checks passed.");
    }
    private class WardrobeModel { }
    private class CustomHolder : Holder { }
}

namespace UnityEngine
{
    public class Object { public static implicit operator bool(Object value) => value != null; }
    public class Transform : Object
    {
        public Transform parent;
        public bool Active = true;
        public readonly List<object> Components = new List<object>();
        public T GetComponent<T>() where T : class
        {
            foreach (object component in Components) if (component is T typed) return typed;
            return null;
        }
    }
}
namespace ThunderRoad
{
    public class Holder : UnityEngine.Object { }
    public class Item : UnityEngine.Object { }
}
