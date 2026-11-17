using ContraLike.Core;
using UnityEngine;

namespace ContraLike.Campaign
{
    public static class LevelRun
    {
        private static string scenePath;
        private static bool doubleJumpEnabled;
        private static LevelCompletion activeLevel;

        public static string LevelId { get; private set; }

        internal static void Begin(string id, string path, bool enableDoubleJump)
        {
            LevelId = id;
            scenePath = path;
            doubleJumpEnabled = enableDoubleJump;
        }

        internal static bool CanDoubleJump(string path) => doubleJumpEnabled && scenePath == path;

        internal static void Register(LevelCompletion level)
        {
            var path = level.gameObject.scene.path;
            if (string.IsNullOrEmpty(path)) return;
            activeLevel = level;
            LevelProgress.Current.BeginAttempt(path, level.resetOnExit);
        }

        internal static bool Tracks(string path) => activeLevel != null && activeLevel.gameObject.scene.path == path;

        internal static void RestartAttempt(string path)
        {
            if (!Tracks(path)) return;
            if (activeLevel.fullRestart) LevelProgress.Current.DiscardAttempt(path);
            activeLevel = null;
        }

        internal static void LeaveAttempt(string path)
        {
            if (!Tracks(path)) return;
            if (activeLevel.resetOnExit) LevelProgress.Current.DiscardAttempt(path);
            else LevelProgress.Current.BeginAttempt(path, discardOnExit: false);
            activeLevel = null;
        }

        internal static void FinishAttempt(string path)
        {
            LevelProgress.Current.CompleteAttempt(path, scenePath == path ? LevelId : null);
        }

        internal static string ObjectKey(Transform target)
        {
            var key = "";
            var current = target;
            // Cache the authored path; unrelated root objects may move to DontDestroyOnLoad.
            while (current.parent != null)
            {
                key = "/" + current.GetSiblingIndex() + key;
                current = current.parent;
            }
            var ordinal = 0;
            foreach (var root in current.gameObject.scene.GetRootGameObjects())
                if (root.name == current.name && root.transform.GetSiblingIndex() < current.GetSiblingIndex()) ordinal++;
            return current.name.Length + ":" + current.name + "/" + ordinal + key;
        }

        internal static bool WasConsumed(string path, string key)
        {
            return Tracks(path) && LevelProgress.Current.WasConsumed(path, key);
        }

        internal static bool RecordReward(string path, string key, int amount)
        {
            if (GameManager.Instance?.State == GameState.Loading) return false;
            if (Tracks(path)) return LevelProgress.Current.RecordReward(path, key, amount);
            LevelProgress.Current.AddCoins(amount);
            return true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void Reset()
        {
            activeLevel = null;
            Begin(null, null, false);
        }
    }
}
