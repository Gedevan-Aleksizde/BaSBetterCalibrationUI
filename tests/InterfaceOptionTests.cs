using System;
using System.Collections.Generic;
using BetterCalibrationUI;

// Compile the real option/attachment implementation against an in-memory scene inventory.
internal static class InterfaceOptionTests
{
    private static int checks;
    private static void Check(bool value) { if (!value) throw new Exception("Interface toggle regression"); checks++; }
    private static void Main()
    {
        var active = new ThunderRoad.TrackersPage();
        var inactive = new ThunderRoad.TrackersPage();
        var asset = new ThunderRoad.TrackersPage();
        asset.gameObject.scene.Valid = false;
        UnityEngine.Resources.Pages.AddRange(new[] { active, inactive, asset });
        var mod = new BCUIChanger();
        Check(BCUIChanger.VRChatInterface);
        mod.ScriptEnable();
        Check(active.gameObject.Mirror.enabled && inactive.gameObject.Mirror.enabled);
        Check(asset.gameObject.Mirror == null);
        var original = active.gameObject.Mirror;
        BCUIChanger.VRChatInterface = false;
        Check(!original.enabled && !inactive.gameObject.Mirror.enabled);
        var later = new ThunderRoad.TrackersPage();
        UnityEngine.Resources.Pages.Add(later);
        ThunderRoad.EventManager.Load();
        Check(later.gameObject.Mirror == null);
        BCUIChanger.VRChatInterface = true;
        Check(original.enabled && later.gameObject.Mirror.enabled);
        Check(ReferenceEquals(original, active.gameObject.Mirror));
        BCUIChanger.VRChatInterface = true;
        Check(ReferenceEquals(original, active.gameObject.Mirror));
        mod.ScriptDisable();
        Check(original.Destroyed && !BCUIChanger.IsRunning);
        BCUIChanger.VRChatInterface = false;
        var disabledPage = new ThunderRoad.TrackersPage();
        UnityEngine.Resources.Pages.Add(disabledPage);
        mod.ScriptEnable();
        Check(disabledPage.gameObject.Mirror == null);
        mod.ScriptDisable();
        BCUIChanger.VRChatInterface = true;
        Check(disabledPage.gameObject.Mirror == null);
        mod.ScriptEnable();
        Check(disabledPage.gameObject.Mirror.enabled);
        mod.ScriptDisable();
        Console.WriteLine("Interface option: " + checks + " lifecycle checks passed.");
    }
}

namespace UnityEngine
{
    public class Object
    {
        public bool Destroyed;
        public static implicit operator bool(Object value) => value != null && !value.Destroyed;
        public static void Destroy(Object value) { value.Destroyed = true; }
    }
    public sealed class Scene { public bool Valid = true; public bool IsValid() => Valid; }
    public sealed class GameObject
    {
        public readonly Scene scene = new Scene();
        public CalibrationMirror Mirror;
        public T GetComponent<T>() where T : class => Mirror as T;
        public T AddComponent<T>() where T : CalibrationMirror, new() { Mirror = new T(); return (T)Mirror; }
    }
    public static class Resources
    {
        public static readonly List<ThunderRoad.TrackersPage> Pages = new List<ThunderRoad.TrackersPage>();
        public static T[] FindObjectsOfTypeAll<T>()
        {
            var result = new List<T>();
            foreach (var page in Pages)
            {
                if (page is T item) result.Add(item);
                if (page.gameObject.Mirror && page.gameObject.Mirror is T mirror) result.Add(mirror);
            }
            return result.ToArray();
        }
    }
    public static class Debug { public static void Log(string message) { } }
}

namespace BetterCalibrationUI
{
    public class CalibrationMirror : UnityEngine.Object { public bool enabled = true; }
}

namespace ThunderRoad
{
    public abstract class ThunderScript { public virtual void ScriptEnable() { } public virtual void ScriptDisable() { } }
    public class TrackersPage : UnityEngine.Object
    {
        public readonly UnityEngine.GameObject gameObject = new UnityEngine.GameObject();
        public T GetComponent<T>() where T : class => gameObject.GetComponent<T>();
    }
    public enum EventTime { OnStart, OnEnd }
    public class LevelData { public string id; public class Mode { } }
    public static class EventManager
    {
        public static event Action<LevelData, LevelData.Mode, EventTime> onLevelLoad;
        public static void Load() => onLevelLoad?.Invoke(new LevelData { id = "mainmenu" }, null, EventTime.OnEnd);
    }
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class ModOptionCategoryAttribute : Attribute { public ModOptionCategoryAttribute(string name, int order, string localization) { } }
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class ModOptionTooltipAttribute : Attribute { public ModOptionTooltipAttribute(string text, string localization) { } }
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class ModOption : Attribute
    {
        public enum InteractionType { ButtonList }
        public int defaultValueIndex;
        public InteractionType interactionType;
        public ModOption(string name, string localization) { }
    }
}
