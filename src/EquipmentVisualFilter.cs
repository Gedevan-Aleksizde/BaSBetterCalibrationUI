using ThunderRoad;
using UnityEngine;

namespace BetterCalibrationUI
{
    internal static class EquipmentVisualFilter
    {
        internal static bool IsExcluded(Transform renderer, Transform avatarRoot)
        {
            // Holders can sit below physics parts or animation bones; item renderers may be skinned.
            // Inspect the full ancestry inside the avatar, including inactive slot objects.
            for (Transform current = renderer; current; current = current.parent)
            {
                if (current.GetComponent<Holder>() || current.GetComponent<Item>()) return true;
                if (current == avatarRoot) break;
            }
            return false;
        }
    }
}
