using System;
using System.Collections;
using ThunderRoad;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace BetterCalibrationUI
{
    // be careful it's dirty implementation
    public class Changer: LevelModule
    {
        public override IEnumerator OnLoadCoroutine()
        {
            EventManager.onLevelLoad += new EventManager.LevelLoadEvent(this.BCUI_onLevelLoad);
            return base.OnLoadCoroutine();
        }
        public override void OnUnload()
        {
            EventManager.onLevelLoad -= new EventManager.LevelLoadEvent(this.BCUI_onLevelLoad);
            base.OnUnload();
        }
        public void BCUI_onLevelLoad(LevelData levelData, EventTime eventTime)
        {
            if(levelData.id == "CharacterSelection" && eventTime == EventTime.OnEnd)
            {
                // if (this.showLine || this.centerButton || setMirror) Debug.Log("[Better Calibration UI] Start");
                if (this.centerButton) MoveButtonToCenter();
                if (this.showFootprints) replaceText();
                // Debug.Log("[Better Calibration UI] Finished");
            }
        }
        private void replaceText()
        {
            foreach (Text t in Resources.FindObjectsOfTypeAll(typeof(Text)) as Text[])
            {
                if(t.gameObject.name == "FootCanvas")
                {
                    t.transform.gameObject.transform.Translate(0, 0, -0.1f);
                    t.text = "Ignore Foot Polygons. Just align trackers with footprints.";
                    Catalog.InstantiateAsync(
                        "calibrationUI.betterFootCanvas",
                        new Vector3(0, 0, 0),
                        Quaternion.Euler(90f, 0, 0),
                        t.transform.parent,
                        delegate(GameObject go) {
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
                    Transform ch = go.transform.Find("04 Character Height");
                    Transform uiCharHeightRight = go.transform.Find("04 Character Height/UIColliderRight");
                    Transform colliderRight = go.transform.Find("04 Character Height/ui_CharHeight_Right");
                    uiCharHeightRight.Translate(this.buttonOffsetX, 0, this.buttonOffsetZ);
                    uiCharHeightRight.Rotate(new Vector3(0, -45, 0));
                    colliderRight.Translate(this.buttonOffsetX, 0, this.buttonOffsetZ);
                    colliderRight.Rotate(new Vector3(0, -45, 0));
                    break;
                }
            }
            // Debug.Log("[Stand Here When Calibrate Trackers] height adjuster shifted");
        }
        public bool showFootprints = true;
        public bool centerButton = true;
        public float buttonOffsetX = -1.3f;
        public float buttonOffsetZ = -1.9f;
        public bool setMirror = true; // TODO: for hip or other extra trackers in the future
    }
}
