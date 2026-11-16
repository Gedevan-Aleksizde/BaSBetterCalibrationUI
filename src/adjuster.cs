using ThunderRoad;
using UnityEngine;

namespace BetterCalibrationUI
{
    public class BCUIChanger : ThunderScript
    {
        private static bool vrChatInterface = true;
        internal static bool IsRunning { get; private set; }

        [ModOptionCategory("General", 0, "BetterCalibrationUIOptCat.General")]
        [ModOption("VRChat-style Interface", "BetterCalibrationUIOpt.VRChatInterface", defaultValueIndex=1, interactionType=ModOption.InteractionType.ButtonList)]
        [ModOptionTooltip("A", "BetterCalibrationUIOpt.VRChatInterface_desc")]
        public static bool VRChatInterface
        {
            get => vrChatInterface;
            set
            {
                if (vrChatInterface == value) return;
                vrChatInterface = value;
                if (IsRunning) Attach();
            }
        }

        [ModOptionCategory("General", 0, "BetterCalibrationUIOptCat.General")]
        [ModOption("Show Bones", "BetterCalibrationUIOpt.ShowBones", defaultValueIndex=1, interactionType=ModOption.InteractionType.ButtonList)]
        [ModOptionTooltip("A", "BetterCalibrationUIOpt.ShowBones_desc")]
        public static bool showBones = true;
        public override void ScriptEnable()
        {
            base.ScriptEnable();
            IsRunning = true;
            Debug.Log("[BetterCalibrationUI] Enabled " + GetType().Assembly.GetName().Version + ".");
            EventManager.onLevelLoad += LevelLoaded;
            Attach();
        }
        public override void ScriptDisable()
        {
            IsRunning = false;
            EventManager.onLevelLoad -= LevelLoaded;
            foreach (CalibrationMirror mirror in Resources.FindObjectsOfTypeAll<CalibrationMirror>())
            { mirror.enabled = false; Object.Destroy(mirror); }
            base.ScriptDisable();
        }
        private void LevelLoaded(LevelData data, LevelData.Mode mode, EventTime time)
        { if (time == EventTime.OnEnd && string.Equals(data.id, "MainMenu", System.StringComparison.OrdinalIgnoreCase)) Attach(); }
        private static void Attach()
        {
            foreach (TrackersPage page in Resources.FindObjectsOfTypeAll<TrackersPage>())
            {
                if (!page.gameObject.scene.IsValid()) continue;
                CalibrationMirror mirror = page.GetComponent<CalibrationMirror>();
                if (!VRChatInterface)
                {
                    if (mirror) mirror.enabled = false;
                    continue;
                }
                if (!mirror)
                {
                    page.gameObject.AddComponent<CalibrationMirror>();
                    Debug.Log("[BetterCalibrationUI] Attached preview controller to tracker page.");
                }
                else mirror.enabled = true;
            }
        }
    }
}
