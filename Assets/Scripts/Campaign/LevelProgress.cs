using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ContraLike.Campaign
{
    // Save data has no Inspector surface. Configuration stays on the authored map.
    public sealed class LevelProgress
    {
        [Serializable]
        private sealed class SaveData
        {
            public int version = 1;
            public List<string> completedLevels = new List<string>();
            public bool doubleJumpDisabled;
            public int coins;
            public List<Attempt> attempts = new List<Attempt>();
            public List<string> purchasedWeapons = new List<string>();
        }

        [Serializable]
        private sealed class Attempt
        {
            public string scenePath;
            public bool discardOnExit;
            public int earnedCoins;
            public List<string> consumed = new List<string>();
        }

        private static LevelProgress current;
        private readonly string savePath;
        private readonly HashSet<string> completed = new HashSet<string>();
        private readonly Dictionary<string, Attempt> attempts = new Dictionary<string, Attempt>();
        private readonly HashSet<string> purchasedWeapons = new HashSet<string>();

        public static LevelProgress Current => current ??= LoadSession();
        public bool DoubleJumpEnabled { get; private set; } = true;
        public int Coins { get; private set; }
        public int SpendableCoins
        {
            get
            {
                long pending = 0;
                foreach (var attempt in attempts.Values) pending += attempt.earnedCoins;
                return (int)Math.Max(0, Coins - pending);
            }
        }
        public event Action Changed;

        public LevelProgress(string path)
        {
            savePath = path;
            if (!File.Exists(path)) return;
            try
            {
                var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
                if (data == null) return;
                DoubleJumpEnabled = !data.doubleJumpDisabled;
                Coins = Math.Max(0, data.coins);
                if (data.completedLevels != null)
                    foreach (var id in data.completedLevels)
                        if (!string.IsNullOrWhiteSpace(id)) completed.Add(id);
                if (data.attempts != null)
                {
                    foreach (var attempt in data.attempts)
                    {
                        if (attempt == null || string.IsNullOrEmpty(attempt.scenePath)) continue;
                        attempt.earnedCoins = Math.Max(0, attempt.earnedCoins);
                        attempt.consumed ??= new List<string>();
                        attempts[attempt.scenePath] = attempt;
                    }
                }
                if (data.purchasedWeapons != null)
                    foreach (var id in data.purchasedWeapons)
                        if (!string.IsNullOrWhiteSpace(id)) purchasedWeapons.Add(id);
            }
            catch (Exception exception) when (exception is IOException || exception is ArgumentException)
            {
                Debug.LogWarning("Не удалось прочитать прогресс уровней: " + exception.Message);
            }
        }

        public bool IsCompleted(string id) => !string.IsNullOrEmpty(id) && completed.Contains(id);

        public bool Complete(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || !completed.Add(id)) return false;
            Save();
            Changed?.Invoke();
            return true;
        }

        private static LevelProgress LoadSession()
        {
            var progress = new LevelProgress(Path.Combine(Application.persistentDataPath, "level-progress.json"));
            var discard = new List<string>();
            foreach (var attempt in progress.attempts.Values)
                if (attempt.discardOnExit) discard.Add(attempt.scenePath);

            foreach (var path in discard) progress.RemoveAttempt(path, refund: true);
            if (discard.Count > 0) progress.Save();
            return progress;
        }

        internal void BeginAttempt(string path, bool discardOnExit)
        {
            if (!attempts.TryGetValue(path, out var attempt))
            {
                attempts.Add(path, new Attempt { scenePath = path, discardOnExit = discardOnExit });
                return;
            }
            if (attempt.discardOnExit == discardOnExit) return;
            attempt.discardOnExit = discardOnExit;
            Save();
        }

        internal bool WasConsumed(string path, string key)
        {
            return attempts.TryGetValue(path, out var attempt) && attempt.consumed.Contains(key);
        }

        internal bool RecordReward(string path, string key, int amount)
        {
            if (!attempts.TryGetValue(path, out var attempt))
            {
                AddCoins(amount);
                return true;
            }
            if (attempt.consumed.Contains(key)) return false;

            attempt.consumed.Add(key);
            var credited = Math.Min(int.MaxValue - Coins, Math.Max(0, amount));
            Coins += credited;
            attempt.earnedCoins += credited;
            Save();
            if (credited > 0) Changed?.Invoke();
            return true;
        }

        internal void DiscardAttempt(string path)
        {
            if (!RemoveAttempt(path, refund: true)) return;
            Save();
            Changed?.Invoke();
        }

        internal void CompleteAttempt(string path, string levelId)
        {
            var finished = RemoveAttempt(path, refund: false);
            var unlocked = !string.IsNullOrEmpty(levelId) && completed.Add(levelId);
            if (!finished && !unlocked) return;
            Save();
            Changed?.Invoke();
        }

        private bool RemoveAttempt(string path, bool refund)
        {
            if (!attempts.Remove(path, out var attempt)) return false;
            if (refund) Coins = Math.Max(0, Coins - attempt.earnedCoins);
            return true;
        }

        public void SetDoubleJumpEnabled(bool enabled)
        {
            if (DoubleJumpEnabled == enabled) return;
            DoubleJumpEnabled = enabled;
            Save();
        }

        public void AddCoins(int amount)
        {
            if (amount <= 0 || Coins == int.MaxValue) return;
            Coins = (int)Math.Min(int.MaxValue, (long)Coins + amount);
            Save();
            Changed?.Invoke();
        }

        public bool IsWeaponPurchased(string id) => !string.IsNullOrEmpty(id) && purchasedWeapons.Contains(id);

        internal bool TryPurchaseWeapon(string id, int price)
        {
            if (string.IsNullOrWhiteSpace(id) || price < 0 || SpendableCoins < price || !purchasedWeapons.Add(id)) return false;
            Coins -= price;
            Save();
            Changed?.Invoke();
            return true;
        }

        public void ResetProgress()
        {
            completed.Clear();
            attempts.Clear();
            purchasedWeapons.Clear();
            Coins = 0;
            DoubleJumpEnabled = true;
            Save();
            Changed?.Invoke();
        }

        private void Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(savePath);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                var data = new SaveData
                {
                    completedLevels = new List<string>(completed),
                    doubleJumpDisabled = !DoubleJumpEnabled,
                    coins = Coins,
                    attempts = new List<Attempt>(attempts.Values),
                    purchasedWeapons = new List<string>(purchasedWeapons)
                };
                var temporaryPath = savePath + ".tmp";
                File.WriteAllText(temporaryPath, JsonUtility.ToJson(data, true));
                if (File.Exists(savePath)) File.Replace(temporaryPath, savePath, null);
                else File.Move(temporaryPath, savePath);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                Debug.LogWarning("Прогресс доступен в текущем запуске, но не сохранён: " + exception.Message);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession() => current = null;
    }
}
