using ThunderRoad;
using UnityEngine.SceneManagement;
using UnityEngine;
using TMPro;

namespace BetterCalibrationUI
{
    // be careful it's dirty implementation
    public class BCUIChanger: ThunderScript
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
                if (modData?.thunderScripts.Contains(this) ?? false)
                {
                    this.BCUI_onLevelLoad(Level.current.data, EventTime.OnEnd);
                }
            }
        }
        public void BCUI_onLevelLoad(LevelData levelData, EventTime eventTime)
        {
            if (levelData.id == "MainMenu" && eventTime == EventTime.OnEnd)
            {
                Debug.Log("onLevelLoad called");
                // if (this.showLine || this.centerButton || setMirror) Debug.Log("[Better Calibration UI] Start");
                if (centerButton)this.MoveButtonToCenter();
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
                    // Transform ch = go.transform.Find("07 Character selection");
                    Transform uiCharHeightRight = go.transform.Find("07 Character Height/UIColliderRight");
                    Transform colliderRight = go.transform.Find("07 Character Height/ui_CharHeight_Right");
                    uiCharHeightRight.Translate(buttonOffsetBaseX + buttonOffsetX, 0, buttonOffsetBaseZ + buttonOffsetZ);
                    uiCharHeightRight.Rotate(new Vector3(0, -45, 0));
                    colliderRight.Translate(buttonOffsetBaseX + buttonOffsetX, 0, buttonOffsetBaseZ + buttonOffsetZ);
                    colliderRight.Rotate(new Vector3(0, -45, 0));
                    break;
                }
            }
            // Debug.Log("[Stand Here When Calibrate Trackers] height adjuster shifted");
        }
        protected const float buttonOffsetBaseX = -1.3f;
        protected const float buttonOffsetBaseZ = -1.9f;
        [ModOption(
            category ="0 General",
            name = "Show Footprints",
            nameLocalizationId = "ModTooltips.BCUI_show_footprint",
            tooltip = "whether or not to show the footprints image when calibrating",
            tooltipLocalizationId = "ModTooltips.BCUI_show_footprint_desc",
            defaultValueIndex = 1)]
        public static bool showFootprints = true;
        [ModOption(
            category = "0 General",
            name = "Center the Button",
            nameLocalizationId = "ModTooltips.BCUI_center_button",
            tooltip = "whether or not to centerize the calibration button",
            tooltipLocalizationId = "ModTooltips.BCUI_center_button_desc",
            defaultValueIndex = 1)]
        public static bool centerButton = true;
        [ModOption(
            category = "1 Calibration Button",
            order =0,
            name = "Button Offset (X-Axis)",
            nameLocalizationId = "ModTooltips.BCUI_offset_x",
            tooltip = "X-axis offset of the calibration button.",
            tooltipLocalizationId = "ModTooltips.BCUI_offset_x_desc")]
        public static float buttonOffsetX = 0f;
        [ModOption(
            category = "1 Calibration Button", order =1,
            name = "Button Offset (Y-Axis)",
            nameLocalizationId = "ModTooltips.BCUI_offset_y",
            tooltip = "Y-axis offset of the calibration button",
            tooltipLocalizationId = "ModTooltips.BCUI_offset_y_desc")]
        public static float buttonOffsetZ = 0f;
        // public static bool setMirror = true; // TODO: for hip or other extra trackers in the future
    }
}
