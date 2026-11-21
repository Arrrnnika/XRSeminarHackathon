using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// Shows the logged findings on a TextMeshPro text.
/// Used in two places:
///  - Control room monitor: Max Entries = 0 (all), Newest First = off
///  - Headset HUD (bottom left): Max Entries = 4, Newest First = on
/// Updates automatically when the scene opens and whenever a new entry is logged.
/// </summary>
public class InspectionLogDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [Tooltip("How many entries to show. 0 = all.")]
    [SerializeField] private int maxEntries = 0;
    [SerializeField] private bool newestFirst = false;
    [SerializeField] private string emptyText = "No findings yet.";

    private void OnEnable()
    {
        InspectionLog.EntryAdded += OnEntryAdded;
        Refresh();
    }

    private void OnDisable()
    {
        InspectionLog.EntryAdded -= OnEntryAdded;
    }

    private void OnEntryAdded(InspectionEntry entry) => Refresh();

    public void Refresh()
    {
        var entries = InspectionLog.Entries;
        if (entries.Count == 0)
        {
            text.text = emptyText;
            return;
        }

        int count = maxEntries > 0 ? Mathf.Min(maxEntries, entries.Count) : entries.Count;
        var builder = new StringBuilder();

        for (int i = 0; i < count; i++)
        {
            //newest first: walk backwards from the end; otherwise show the last 'count' entries in order
            int index = newestFirst ? entries.Count - 1 - i : entries.Count - count + i;
            InspectionEntry entry = entries[index];
            builder.AppendLine($"{entry.time}  {entry.spotId}: {entry.action}");
        }

        text.text = builder.ToString();
    }
}