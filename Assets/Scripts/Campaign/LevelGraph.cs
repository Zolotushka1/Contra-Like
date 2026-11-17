using System;
using System.Collections.Generic;

namespace ContraLike.Campaign
{
    public static class LevelGraph
    {
        // Multiple incoming links mean ANY completed predecessor unlocks this node.
        public static bool IsAvailable(LevelNode node, IReadOnlyList<LevelNode> levels, Func<string, bool> isCompleted)
        {
            if (node == null) return false;
            if (node.availableFromStart || isCompleted(node.Id)) return true;
            foreach (var previous in levels)
                if (previous != null && isCompleted(previous.Id) && previous.nextLevels != null && previous.nextLevels.Contains(node))
                    return true;
            return false;
        }
    }
}
