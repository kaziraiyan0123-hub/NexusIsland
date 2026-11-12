using System;
using System.Collections.ObjectModel;
using System.Linq;
using Clipboard = System.Windows.Clipboard;
using NexusIsland.Core;

namespace NexusIsland.Services;

public class ClipboardService
{
    public ObservableCollection<ClipboardEntry> History { get; } = new();
    public event Action? ClipboardUpdated;

    public void Initialize(System.Collections.Generic.IEnumerable<ClipboardEntry> savedPinned)
    {
        foreach (var item in savedPinned)
        {
            History.Add(item);
        }
    }

    public void OnClipboardUpdated()
    {
        try
        {
            if (Clipboard.ContainsText())
            {
                string text = Clipboard.GetText().Trim();
                if (string.IsNullOrWhiteSpace(text)) return;

                // Check if already top item
                var top = History.FirstOrDefault();
                if (top != null && top.Text == text)
                {
                    return;
                }

                // Remove existing if duplicate
                var existing = History.FirstOrDefault(x => x.Text == text);
                if (existing != null && !existing.IsPinned)
                {
                    History.Remove(existing);
                }

                var entry = new ClipboardEntry
                {
                    Text = text,
                    CopiedAt = DateTime.Now,
                    IsPinned = false
                };

                // Insert below pinned items or at top
                int insertIndex = History.TakeWhile(x => x.IsPinned).Count();
                History.Insert(insertIndex, entry);

                // Cap unpinned history to 50 items
                while (History.Count(x => !x.IsPinned) > 50)
                {
                    var lastUnpinned = History.LastOrDefault(x => !x.IsPinned);
                    if (lastUnpinned != null)
                    {
                        History.Remove(lastUnpinned);
                    }
                }

                ClipboardUpdated?.Invoke();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Clipboard read failed: {ex.Message}");
        }
    }

    public void CopyToClipboard(string text)
    {
        try
        {
            Clipboard.SetText(text);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Clipboard set failed: {ex.Message}");
        }
    }

    public void TogglePin(ClipboardEntry entry)
    {
        entry.IsPinned = !entry.IsPinned;
        History.Remove(entry);
        if (entry.IsPinned)
        {
            History.Insert(0, entry);
        }
        else
        {
            int insertIndex = History.TakeWhile(x => x.IsPinned).Count();
            History.Insert(insertIndex, entry);
        }
        ClipboardUpdated?.Invoke();
    }

    public void RemoveEntry(ClipboardEntry entry)
    {
        History.Remove(entry);
        ClipboardUpdated?.Invoke();
    }

    public void ClearUnpinned()
    {
        var unpinned = History.Where(x => !x.IsPinned).ToList();
        foreach (var item in unpinned)
        {
            History.Remove(item);
        }
        ClipboardUpdated?.Invoke();
    }
}
