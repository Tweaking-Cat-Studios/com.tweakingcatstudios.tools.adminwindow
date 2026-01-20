using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace com.tcs.tools.adminwindow.Core.Persistence
{
    [System.Serializable] public class HistoryData { public List<string> Items = new(); }

    public static class AdminHistoryStore
    {
        const int Max = 200;
        static HistoryData _data;

        static AdminHistoryStore()
        {
            _data = new HistoryData();
            try
            {
                if (File.Exists(AdminPaths.HistoryFile))
                    _data = JsonUtility.FromJson<HistoryData>(File.ReadAllText(AdminPaths.HistoryFile)) ?? new HistoryData();
            }
            catch { _data = new HistoryData(); }
        }

        public static IReadOnlyList<string> All => _data.Items;

        public static void Push(string cmd)
        {
            if (string.IsNullOrWhiteSpace(cmd)) return;
            _data.Items.Remove(cmd);           // Remove if exists
            _data.Items.Insert(0, cmd);        // Insert at top
            while (_data.Items.Count > Max) _data.Items.RemoveAt(_data.Items.Count - 1); // Remove oldest
            Save();
        }

        public static void Clear()
        {
            _data.Items.Clear();
            Save();
        }

        static void Save()
        {
            try { File.WriteAllText(AdminPaths.HistoryFile, JsonUtility.ToJson(_data, true)); }
            catch { /* ignore */ }
        }
    }
}