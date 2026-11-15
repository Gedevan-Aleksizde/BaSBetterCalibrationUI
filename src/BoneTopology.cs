using System;
using System.Collections.Generic;

namespace BetterCalibrationUI
{
    internal static class BoneTopology
    {
        internal static int[] Parents<T>(IList<T> bones, Func<T, T> parentOf, T root) where T : class
        {
            var indices = new Dictionary<T, int>();
            for (int i = 0; i < bones.Count; i++) indices.Add(bones[i], i);
            var parents = new int[bones.Count];
            for (int i = 0; i < bones.Count; i++)
            {
                T parent = parentOf(bones[i]);
                if (EqualityComparer<T>.Default.Equals(parent, root)) parents[i] = -1;
                else if (parent == null || !indices.TryGetValue(parent, out parents[i]))
                    throw new InvalidOperationException("A render bone's animation parent is missing.");
            }
            for (int i = 0; i < bones.Count; i++)
            {
                var visited = new HashSet<int>();
                for (int at = i; at != -1; at = parents[at])
                    if (!visited.Add(at)) throw new InvalidOperationException("Cyclic render skeleton.");
            }
            return parents;
        }
    }
}
