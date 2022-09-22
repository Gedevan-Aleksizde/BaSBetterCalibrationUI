using System;
using System.Collections;
using ThunderRoad;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine;

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
            if(levelData.id == "CharacterSelection")
            {
                // if (this.showLine || this.centerButton || setMirror) Debug.Log("[Better Calibration UI] Start");
                if (this.centerButton) moveButtonToCenter();
                if (this.showFootprints) replaceText();
                // Debug.Log("[Better Calibration UI] Finished");
            }
        }
        private void replaceText()
        {
            const float feetDecenteringOffset = 0.28f;
            const float feetForwardOffset = 0.19f;
            const float footAngle = 17f;
            const float scaleImage = 0.95f;
            foreach (Text t in Resources.FindObjectsOfTypeAll(typeof(Text)) as Text[])
            {
                if(t.gameObject.name == "FootCanvas")
                {
                    t.transform.gameObject.transform.Translate(0, 0, -0.1f);
                    t.text = "Ignore Foot Polygons. Just align trackers with footprints.";
                    GameObject betterFootCanvas = GameObject.Instantiate(t.gameObject, new Vector3(0, 1.0f, -0.16f), Quaternion.Euler(90f, 0, 0)) as GameObject;
                    betterFootCanvas.name = "betterFootCanvas";
                    betterFootCanvas.transform.SetParent(t.transform.parent);
                    Text t2text = betterFootCanvas.GetComponent<Text>();
                    t2text.text = "__________________________________";
                    t2text.horizontalOverflow = HorizontalWrapMode.Overflow;
                    t2text.verticalOverflow = VerticalWrapMode.Overflow;
                    GameObject uiFeet = new GameObject("UIFeet");
                    uiFeet.AddComponent<RectTransform>();
                    uiFeet.AddComponent<Canvas>();
                    uiFeet.AddComponent<CanvasScaler>();
                    uiFeet.transform.SetParent(betterFootCanvas.transform, false); // worldPositionStays って誰が得するの
                    uiFeet.transform.Translate(new Vector3(0, feetForwardOffset, 0));
                    uiFeet.transform.eulerAngles = new Vector3(90f, 0, 0);
                    uiFeet.transform.localScale = new Vector3(scaleImage, scaleImage, scaleImage);
                    GameObject uiLeftFoot = new GameObject("UILeftFoot");
                    GameObject uiRightFoot = new GameObject("UIRightFoot");
                    uiLeftFoot.transform.SetParent(uiFeet.transform, false);
                    uiRightFoot.transform.SetParent(uiFeet.transform, false);
                    uiLeftFoot.transform.Rotate(new Vector3(0, 0, footAngle));
                    uiRightFoot.transform.Rotate(new Vector3(0, 0, -footAngle));
                    uiLeftFoot.AddComponent<Image>();
                    uiRightFoot.AddComponent<Image>();
                    uiLeftFoot.transform.Translate(new Vector3(-feetDecenteringOffset, 0, 0));
                    uiRightFoot.transform.Translate(new Vector3(feetDecenteringOffset, 0, 0));
                    Catalog.LoadAssetAsync<Texture2D>("calibrationUI.ui_leftfoot", tex2D => 
                    {
                        uiLeftFoot.GetComponent<Image>().sprite = Sprite.Create(tex2D, new Rect(0f, 0f, tex2D.width, tex2D.height), new Vector2(-.5f, .5f));
                    }, uiLeftFoot.name);
                    Catalog.LoadAssetAsync<Texture2D>("calibrationUI.ui_rightfoot", tex2D => {
                        uiRightFoot.GetComponent<Image>().sprite = Sprite.Create(tex2D, new Rect(0f, 0f, tex2D.width, tex2D.height), new Vector2(.5f, .5f));
                    }, uiRightFoot.name);
                    break;
                }
            }
            // Debug.Log("[Stand Here When Calibrate Trackers] draw the line");
        }
        private void moveButtonToCenter()
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
