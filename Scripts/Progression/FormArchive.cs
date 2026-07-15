using System.Collections.Generic;
using System.Linq;
using ProjectMannequin.Core;
using ProjectMannequin.Data;

namespace ProjectMannequin.Progression;

public sealed class FormArchive
{
    private readonly Dictionary<string, CharacterData> _unlockedForms = new();
    private readonly List<string> _activeLoadout = new();

    public int ActiveFormLimit { get; set; } = 3;
    public IReadOnlyDictionary<string, CharacterData> UnlockedForms => _unlockedForms;
    public IReadOnlyList<string> ActiveLoadout => _activeLoadout;

    public bool UnlockForm(CharacterData form)
    {
        if (string.IsNullOrWhiteSpace(form.Id) || _unlockedForms.ContainsKey(form.Id))
        {
            return false;
        }

        _unlockedForms.Add(form.Id, form);

        if (_activeLoadout.Count < ActiveFormLimit)
        {
            _activeLoadout.Add(form.Id);
        }

        return true;
    }

    public CharacterData? GetForm(string formId)
    {
        return _unlockedForms.TryGetValue(formId, out var form) ? form : null;
    }

    public bool CanUse(string formId)
    {
        return _activeLoadout.Contains(formId);
    }

    public CharacterData? GetNextEquippedForm(string currentFormId)
    {
        if (_activeLoadout.Count == 0)
        {
            return null;
        }

        var currentIndex = _activeLoadout.IndexOf(currentFormId);
        var nextIndex = currentIndex < 0
            ? 0
            : (currentIndex + 1) % _activeLoadout.Count;

        return GetForm(_activeLoadout[nextIndex]);
    }

    public bool TryEquip(string formId)
    {
        if (!_unlockedForms.ContainsKey(formId))
        {
            return false;
        }

        if (_activeLoadout.Contains(formId))
        {
            return true;
        }

        if (_activeLoadout.Count >= Math.Min(ActiveFormLimit, GameConstants.MaxPlayers))
        {
            return false;
        }

        _activeLoadout.Add(formId);
        return true;
    }

    public bool Unequip(string formId)
    {
        // Keep at least one form equipped so the actor always has a usable form.
        if (_activeLoadout.Count <= 1)
        {
            return false;
        }

        return _activeLoadout.Remove(formId);
    }

    public void SetActiveLoadout(IEnumerable<string> formIds)
    {
        var restored = formIds
            .Where(formId => _unlockedForms.ContainsKey(formId))
            .Distinct()
            .Take(ActiveFormLimit)
            .ToList();
        if (restored.Count == 0)
        {
            return;
        }

        _activeLoadout.Clear();
        _activeLoadout.AddRange(restored);
    }
}
