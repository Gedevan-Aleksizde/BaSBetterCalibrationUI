using System;

namespace BetterCalibrationUI
{
    internal enum TrackerRole { None = -1, Waist = 0, LeftFoot = 1, RightFoot = 2, LeftKnee = 3, RightKnee = 4, LeftElbow = 5, RightElbow = 6 }

    internal static class TrackerPolicy
    {
        internal static TrackerRole Parse(string controllerType)
        {
            switch ((controllerType ?? "").ToLowerInvariant())
            {
                case "vive_tracker_waist": return TrackerRole.Waist;
                case "vive_tracker_left_foot": return TrackerRole.LeftFoot;
                case "vive_tracker_right_foot": return TrackerRole.RightFoot;
                case "vive_tracker_left_knee": return TrackerRole.LeftKnee;
                case "vive_tracker_right_knee": return TrackerRole.RightKnee;
                case "vive_tracker_left_elbow": return TrackerRole.LeftElbow;
                case "vive_tracker_right_elbow": return TrackerRole.RightElbow;
                default: return TrackerRole.None;
            }
        }

        internal static bool CanAssign(TrackerRole role, float distance)
        {
            return role >= TrackerRole.Waist && role <= TrackerRole.RightElbow && !float.IsNaN(distance) && distance >= 0f && distance <= .5f;
        }

        internal static TrackerRole Resolve(string controllerType, string inputProfile)
        {
            TrackerRole role = Parse(controllerType);
            if (role != TrackerRole.None) return role;
            if (controllerType != "vive_tracker" && controllerType != "tundra_tracker" && !string.IsNullOrEmpty(controllerType)) return TrackerRole.None;
            string name = (inputProfile ?? "").Replace('\\', '/');
            name = name.Substring(name.LastIndexOf('/') + 1);
            if (!name.EndsWith("_profile.json", StringComparison.OrdinalIgnoreCase)) return TrackerRole.None;
            name = name.Substring(0, name.Length - "_profile.json".Length);
            if (name.StartsWith("tundra_tracker_", StringComparison.Ordinal)) name = "vive_tracker_" + name.Substring("tundra_tracker_".Length);
            return Parse(name);
        }
    }
}
