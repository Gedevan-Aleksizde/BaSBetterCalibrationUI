using ThunderRoad;
using TMPro;
using UnityEngine;

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
                if (showFootprints) this.ChangeSignOnFloor();
                this.ChangeCamera();
                Camera.main.gameObject.AddComponent<RotateCamera>().Setup(Player.local.transform, Camera.main.transform);
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
                        delegate (GameObject go)
                        {
                            go.transform.localPosition = new Vector3(0, 0.035f, -0.16f);
                        }, "loadFootPrint");
                    break;
                }
            }
        }
        public void ChangeCamera()
        {
            // キャラクター選択画面で spectator camera に切り替えられるか?
            for (int i = 0; i < Camera.allCameras.Length; i++)
            {
                Debug.Log($"camera={Camera.allCameras[i].name}");
            }
            Debug.Log($"spectator cam={Spectator.local?.cam?.name}");
        }
        protected const float buttonOffsetBaseX = -1.3f;
        protected const float buttonOffsetBaseZ = -2.0f;
        [ModOptionCategory("General", 0, "CalibrationUI.category_general")]
        [ModOption(name = "show_footprints", nameLocalizationId = "CalibrationUI.BCUI_show_footprint", defaultValueIndex = 1, interactionType = ModOption.InteractionType.ButtonList)]
        [ModOptionTooltip("A", "CalibrationUI.BCUI_show_footprint_desc")]
        public static bool showFootprints = true;
        // public static bool setMirror = true; // TODO: for hip or other extra trackers in the future
        [ModOptionCategory("General", 0, "CalibrationUI.category_general")]
        [ModOption("rotate_camera", "CalibrationUI.rotate_camera", defaultValueIndex = 0, interactionType = ModOption.InteractionType.ButtonList)]
        [ModOptionTooltip("A", "CalibrationUI.BCUI_rotate_camera_desc")]
        public static bool rotateCamera = false;
    }
}
