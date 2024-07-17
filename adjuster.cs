using ThunderRoad;
using UnityEngine.SceneManagement;
using UnityEngine;
using TMPro;

namespace BetterCalibrationUI
{
    // be careful it's dirty implementation
    public class BCUIChanger : ThunderScript
    {
        public override void ScriptLoaded(ModManager.ModData modData)
        {
            base.ScriptLoaded(modData);
        }
        public override void ScriptEnable()
        {
            ModManager.OnModLoad += ModManager_OnModLoad;
            EventManager.onLevelLoad += new EventManager.LevelLoadEvent(this.BCUI_onLevelLoad);
            base.ScriptEnable();
        }
        public override void ScriptDisable()
        {
            ModManager.OnModLoad -= ModManager_OnModLoad;
            EventManager.onLevelLoad -= new EventManager.LevelLoadEvent(this.BCUI_onLevelLoad);
            base.ScriptDisable();
        }
        private void ModManager_OnModLoad(EventTime eventTime, ModManager.ModLoadEventType eventType, ModManager.ModData modData = null)
        {
            if (eventTime == EventTime.OnEnd)
            {
                if (Level.current?.data != null && (modData?.thunderScripts.Contains(this) ?? false))
                {
                    this.BCUI_onLevelLoad(Level.current.data, Level.current.mode, EventTime.OnEnd);
                }
            }
        }
        public void BCUI_onLevelLoad(LevelData levelData, LevelData.Mode mode, EventTime eventTime)
        {
            if (levelData.id == "MainMenu" && eventTime == EventTime.OnEnd)
            {
                // Debug.Log("onLevelLoad called");
                // if (this.showLine || this.centerButton || setMirror) Debug.Log("[Better Calibration UI] Start");
                if (centerButton) this.MoveButtonToCenter();
                if (showFootprints) this.ChangeSignOnFloor();
                // Debug.Log("[Better Calibration UI] Finished");
            }
        }
        private void ChangeSignOnFloor()
        {
            foreach (TextMeshProUGUI t in Resources.FindObjectsOfTypeAll(typeof(TextMeshProUGUI)) as TextMeshProUGUI[])
            {
                if (t.gameObject.name == "FootCanvas")
                {
                    t.transform.gameObject.transform.Translate(0, 0, -0.1f);
                    t.text = "Ignore Foot Polygons. Just align trackers with footprints.";
                    Catalog.InstantiateAsync(
                        "BetterCalibrationUI.betterFootCanvas",
                        new Vector3(0, 0, 0),
                        Quaternion.Euler(90f, 0, 0),
                        t.transform.parent,
                        delegate (GameObject go) {
                            go.transform.localPosition = new Vector3(0, 0.035f, -0.16f);
                        }, "loadFootPrint");
                    break;
                }
            }
        }
        private void MoveButtonToCenter()
        {
            // TODO: GameObject.Find?
            GameObject[] allGameObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (GameObject go in allGameObjects)
            {
                if (go.name == "UI")
                {
                    Transform heightButton = go.transform.Find("07 Character Height Calibration");
                    heightButton .Translate(buttonOffsetBaseX + buttonOffsetX, 0, buttonOffsetBaseZ + buttonOffsetZ);
                    heightButton .Rotate(new Vector3(0, -45, 0));
                    break;
                }
            }
            // Debug.Log("[Stand Here When Calibrate Trackers] height adjuster shifted");
        }
        protected const float buttonOffsetBaseX = -0.3f;
        protected const float buttonOffsetBaseZ = -1.0f;
        [ModOptionCategory("General", 0, "ModOpts.category_general")]
        [ModOption(name = "show_footprints", nameLocalizationId = "ModOpts.BCUI_show_footprint", defaultValueIndex = 1)]
        [ModOptionTooltip("A", "ModOpts.BCUI_show_footprint_desc")]
        public static bool showFootprints = true;
        [ModOptionCategory("General", 0, "ModOpts.category_general")]
        [ModOption(name = "center_Button", nameLocalizationId = "ModOpts.BCUI_center_button", defaultValueIndex = 1)]
        [ModOptionTooltip("A", "ModOpts.BCUI_center_button_desc")]
        public static bool centerButton = true;
        [ModOptionCategory("Calibration Buttions", 1, "ModOpts.category_buttons")]
        [ModOption(order = 0, name = "Button_Offset_x", nameLocalizationId = "ModOpts.BCUI_offset_x", valueSourceName = nameof(OffsetValues), defaultValueIndex = 10, interactionType = ModOption.InteractionType.Slider)]
        [ModOptionTooltip("A", "ModOpts.BCUI_offset_x_desc")]
        public static float buttonOffsetX = 0f;
        [ModOptionCategory("Calibration Buttions", 1, "ModOpts.category_buttons")]
        [ModOption(category = "Calibration Button", order = 1, name = "button_offset_y", nameLocalizationId = "ModOpts.BCUI_offset_y", valueSourceName = nameof(OffsetValues), defaultValueIndex = 10, interactionType = ModOption.InteractionType.Slider)]
        [ModOptionTooltip("A", "ModOpts.BCUI_offset_y_desc")]
        public static float buttonOffsetZ = 0f;
        private static ModOptionFloat[] OffsetValues()
        {
            ModOptionFloat[] vals = new ModOptionFloat[21];
            for(int i = 0; i < 21; i++)
            {
                vals[i] = new ModOptionFloat(((i - 10)/10f).ToString("F1"), ((i - 10)/10f));
            }
            return vals;
        }
        // public static bool setMirror = true; // TODO: for hip or other extra trackers in the future
    }
}
