using ThunderRoad;
using UnityEngine;

namespace BetterCalibrationUI
{
    public class BCUIChanger : ThunderScript
    {
        [ModOptionCategory("General", 0, "BetterCalibrationUIOptCat.General")]
        [ModOption("Show Bones", "BetterCalibrationUIOpt.ShowBones", defaultValueIndex=1, interactionType=ModOption.InteractionType.ButtonList)]
        [ModOptionTooltip("A", "BetterCalibrationUIOpt.ShowBones_desc")]
        public static bool showBones = true;
        public override void ScriptEnable()
        {
            base.ScriptEnable();
            Debug.Log("[BetterCalibrationUI] Enabled 3.2.2.");
            EventManager.onLevelLoad += LevelLoaded;
            Attach();
        }
        public override void ScriptDisable()
        {
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
                if (page.gameObject.scene.IsValid() && !page.GetComponent<CalibrationMirror>())
                { page.gameObject.AddComponent<CalibrationMirror>(); Debug.Log("[BetterCalibrationUI] Attached preview controller to tracker page."); }
        }
    }
}
