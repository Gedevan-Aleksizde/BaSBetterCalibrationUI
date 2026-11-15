using ThunderRoad;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace BetterCalibrationUI
{
    internal static class WaistCalibrator
    {
        public static void SetHands()
        {
            if (Player.local != null)
            {
                WaistCalibrator.playerHandLeftTransf = Player.local.handLeft.root; // Player.local.handLeft.transform;
                WaistCalibrator.playerHandRightTransf = Player.local.handRight.root; // Player.local.handRight.transform;
                foreach (Holder hl in Player.local.creature.holders)
                {
                    // TODO: how to expand to MoreSlots?
                    if (hl.drawSlot == Holder.DrawSlot.HipsRight)
                    {
                        WaistCalibrator.waistHolsterLeftTransf = hl.transform;
                    }
                    if (hl.drawSlot == Holder.DrawSlot.HipsLeft)
                    {
                        WaistCalibrator.waistHolsterRightTransf = hl.transform;
                    }
                }
            }
            else
            {
                Debug.LogWarning("Player.local is null!");
            }
        }
        public static void IntializeHandCalibration_OnModLoad(EventTime eventTime, ModManager.ModLoadEventType eventType, ModManager.ModData modData = null)
        {
            if (eventTime == EventTime.OnEnd && eventType == ModManager.ModLoadEventType.ModManager && modData == null)
            {
                WaistCalibrator.InitializeHandCalibrationTransfValues();
            }
        }
        public static void InitializeHandCalibrationTransfValues(float multiplier = 1.0f)
        {
            if (WaistCalibrator.waistHolsterLeftTransf != null)
            {
                WaistCalibrator.ChangeLeftWaist(multiplier);
            }
            else
            {
                Debug.LogWarning("waistHolsterLeftTransf  is null!");
            }
            if (WaistCalibrator.waistHolsterRightTransf != null)
            {
                WaistCalibrator.ChangeRightWaist(multiplier);
            }
            else
            {
                Debug.LogWarning("waistHolsterRightTransf is null!");
            }
        }
        public static void ChangeBothWaist(float multiplier)
        {
            WaistCalibrator.ChangeLeftWaist(multiplier);
            WaistCalibrator.ChangeRightWaist(multiplier);
        }
        static void ChangeLeftWaist(float multiplier)
        {
            WaistCalibrator.waistHolsterLeftTransf.localPosition = Vector3.Scale(Vector3.right + Vector3.forward, waistLeftBasePos) + (Vector3.left * (waistLeftBasePos.x * multiplier));
        }
        static void ChangeRightWaist(float multiplier)
        {
            WaistCalibrator.waistHolsterRightTransf.localPosition = Vector3.Scale(Vector3.right + Vector3.forward, waistRightBasePos) + (Vector3.right * (waistLeftBasePos.x * multiplier));
        }

        private static Vector3 waistLeftBasePos = Vector3.zero;
        private static Vector3 waistRightBasePos = Vector3.zero;
        private static Transform waistHolsterLeftTransf;
        private static Transform waistHolsterRightTransf;
    }
}
