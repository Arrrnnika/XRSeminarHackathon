using System;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class InspectionEntry
{
    public string time;
    public string spotId;
    public string action;
    public Vector3 position;
}

public static class InspectionLog
{
    public const string DamageDetected = "Damage detected";
    public const string Repaired = "Repaired (epoxy applied)";
    
    private static readonly List<InspectionEntry> _entries = new List<InspectionEntry>();
    
    public static event Action<InspectionEntry> EntryAdded;
    
    public static IReadOnlyList<InspectionEntry> Entries => _entries;
    
    //Empty log everytime the app /playmode starts
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        _entries.Clear();
        EntryAdded = null;
    }

    public static void AddEntry(string spotId, string action, Vector3 position)
    {
        var entry = new InspectionEntry
        {
            time = DateTime.Now.ToString("HH:mm:ss"),
            spotId = spotId,
            action = action,
            position = position
        };
        _entries.Add(entry);
        EntryAdded?.Invoke(entry);
    }

    public static bool ContainsEntry(string spotId, string action)
    {
        foreach (InspectionEntry entry in _entries)
        {
            if (entry.spotId == spotId && entry.action == action)
                return true;
        }

        return false;
    }

    //for a "new inspection" button or sum
    public static void ClearEntries()
    {
        _entries.Clear();
    }

}