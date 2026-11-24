using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RecolecEco
{
    /// <summary>Estado persistente de la partida (JSON local, §8).</summary>
    [Serializable]
    public partial class SaveData
    {
        public int version = 1;
        public int credits;
        public bool[] upgrades = new bool[3];
        public int missionIndex;          // 0..4 activa, 5 = campaña completada
        public bool mission1Reached;
        public bool introShown;
        public bool finaleShown;
        public List<string> collectedIds = new List<string>();
        public int[] inventory = new int[5];
        public int[] collectedByType = new int[5];
        public int[] sortedByType = new int[5];
        public int totalSortedCorrect;
        public int creditsEarned;
        public int milestoneReached;      // último hito (%) alcanzado
        public float playTime;
        public string savedAt;

        public bool HasUpgrade(UpgradeType t) => upgrades != null && upgrades.Length > (int)t && upgrades[(int)t];

        public int UpgradeCount
        {
            get
            {
                int n = 0;
                if (upgrades != null) foreach (var u in upgrades) if (u) n++;
                return n;
            }
        }

        public void Sanitize()
        {
            if (upgrades == null || upgrades.Length != 3) upgrades = Resize(upgrades, 3);
            if (inventory == null || inventory.Length != 5) inventory = ResizeInt(inventory, 5);
            if (collectedByType == null || collectedByType.Length != 5) collectedByType = ResizeInt(collectedByType, 5);
            if (sortedByType == null || sortedByType.Length != 5) sortedByType = ResizeInt(sortedByType, 5);
            if (collectedIds == null) collectedIds = new List<string>();
            missionIndex = Mathf.Clamp(missionIndex, 0, 5);
        }

        static bool[] Resize(bool[] a, int n) { var r = new bool[n]; if (a != null) Array.Copy(a, r, Mathf.Min(n, a.Length)); return r; }
        static int[] ResizeInt(int[] a, int n) { var r = new int[n]; if (a != null) Array.Copy(a, r, Mathf.Min(n, a.Length)); return r; }
    }

    public static class SaveManager
    {
        const string FileName = "recolececo_partida.json";
        public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);
        public static bool Exists => File.Exists(FilePath);

        public static bool Save(SaveData data)
        {
            try
            {
                data.savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string json = JsonUtility.ToJson(data, true);
                string tmp = FilePath + ".tmp";
                File.WriteAllText(tmp, json);
                if (File.Exists(FilePath)) File.Delete(FilePath);
                File.Move(tmp, FilePath);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("No se pudo guardar la partida: " + e.Message);
                return false;
            }
        }

        public static SaveData Load()
        {
            try
            {
                if (!Exists) return null;
                var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
                data?.Sanitize();
                return data;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Partida guardada dañada, se ignora: " + e.Message);
                return null;
            }
        }

        public static void Delete()
        {
            try { if (Exists) File.Delete(FilePath); }
            catch (Exception e) { Debug.LogWarning("No se pudo borrar la partida: " + e.Message); }
        }
    }
}
