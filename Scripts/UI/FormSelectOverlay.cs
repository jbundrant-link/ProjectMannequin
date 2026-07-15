using System.Collections.Generic;
using System.Linq;
using Godot;
using ProjectMannequin.Combat;
using ProjectMannequin.Core;
using ProjectMannequin.Data;
using ProjectMannequin.LocalInput;
using ProjectMannequin.Presentation;

namespace ProjectMannequin.UI;

/// <summary>
/// In-fight character-select overlay for form swapping. Opening pauses the tree
/// and presents the player's active loadout as a grid; confirming routes the
/// chosen form through <see cref="CombatStateMachine.RequestFormSwap"/> so the
/// swap still starts inside the deterministic simulation tick.
///
/// Input is handled in <see cref="_Input"/> (not <c>_UnhandledInput</c>) so that
/// while the overlay is open it is fully modal and consumes keys before the
/// pause menu can react to them.
/// </summary>
public partial class FormSelectOverlay : CanvasLayer
{
    [Export] public NodePath SimulationPath { get; set; } = "../GameSimulation";
    [Export] public int Columns { get; set; } = 3;
    [Export] public int Rows { get; set; } = 2;

    private readonly List<FormEntry> _entries = new();
    private readonly List<PanelContainer> _slotPanels = new();

    private AudioStreamPlayer _sfx = null!;
    private AudioStream _openSound = null!;
    private AudioStream _moveSound = null!;
    private AudioStream _confirmSound = null!;
    private AudioStream _cancelSound = null!;
    private float _pulseTime;

    private GameSimulation? _simulation;
    private CombatActor? _player;

    private Control _root = null!;
    private GridContainer _grid = null!;
    private TextureRect _previewPortrait = null!;
    private Label _previewName = null!;
    private Label _previewRole = null!;
    private Label _previewStats = null!;
    private Label _previewTags = null!;

    private int _cursor;
    private bool _isOpen;

    private readonly record struct FormEntry(string Id, CharacterData Form, bool IsCurrent);

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        Layer = 25;
        _simulation = GetNodeOrNull<GameSimulation>(SimulationPath);
        BuildInterface();
        BuildAudio();
        _root.Visible = false;
    }

    public override void _Process(double delta)
    {
        if (!_isOpen)
        {
            return;
        }

        // Pulse the selected slot's border so the cursor reads at a glance.
        _pulseTime += (float)delta;
        if (_cursor >= 0 && _cursor < _slotPanels.Count)
        {
            _slotPanels[_cursor].AddThemeStyleboxOverride(
                "panel",
                SlotStyle(true, _entries[_cursor].IsCurrent, _pulseTime));
        }
    }

    public override void _Input(InputEvent inputEvent)
    {
        if (!_isOpen)
        {
            // Only reacts to the form-swap button while closed; everything else
            // passes through to combat and the rest of the UI untouched.
            if (UiInputRouter.IsPressed(inputEvent, LogicalUiAction.FormSwap) && TryOpen())
            {
                GetViewport().SetInputAsHandled();
            }

            return;
        }

        if (UiInputRouter.IsPressed(inputEvent, LogicalUiAction.FormSwap)
            || UiInputRouter.IsPressed(inputEvent, LogicalUiAction.Cancel))
        {
            PlaySound(_cancelSound);
            Close();
        }
        else if (UiInputRouter.IsPressed(inputEvent, LogicalUiAction.Accept))
        {
            Confirm();
        }
        else if (UiInputRouter.IsPressed(inputEvent, LogicalUiAction.Left))
        {
            MoveCursor(-1, 0);
        }
        else if (UiInputRouter.IsPressed(inputEvent, LogicalUiAction.Right))
        {
            MoveCursor(1, 0);
        }
        else if (UiInputRouter.IsPressed(inputEvent, LogicalUiAction.Up))
        {
            MoveCursor(0, -1);
        }
        else if (UiInputRouter.IsPressed(inputEvent, LogicalUiAction.Down))
        {
            MoveCursor(0, 1);
        }
        else if (inputEvent is not (InputEventKey or InputEventJoypadButton or InputEventJoypadMotion))
        {
            // Let non-key events (mouse motion, etc.) pass through.
            return;
        }

        // Modal: swallow every key/pad event so the pause menu never sees them.
        GetViewport().SetInputAsHandled();
    }

    private bool TryOpen()
    {
        if (_isOpen || GetTree().Paused)
        {
            return false;
        }

        // Never open during replay playback — recorded input masks drive the sim,
        // and a live UI-driven swap would diverge from the recording.
        if (_simulation is not null && _simulation.IsReplayPlayback)
        {
            return false;
        }

        _player = ResolvePlayer();
        if (_player is null || !_player.StateMachine.CanOpenFormSelect)
        {
            return false;
        }

        BuildEntries();
        if (_entries.Count < 2)
        {
            return false;
        }

        var currentIndex = _entries.FindIndex(entry => entry.IsCurrent);
        _cursor = currentIndex < 0 ? 0 : currentIndex;

        BuildGrid();
        UpdateSelectionVisuals();
        UpdatePreview();

        _pulseTime = 0.0f;
        _root.Visible = true;
        _isOpen = true;
        GetTree().Paused = true;
        PlaySound(_openSound);
        return true;
    }

    private void Confirm()
    {
        var swapped = false;
        if (_player is not null && _cursor >= 0 && _cursor < _entries.Count)
        {
            var entry = _entries[_cursor];
            if (!entry.IsCurrent)
            {
                _player.StateMachine.RequestFormSwap(entry.Id);
                swapped = true;
            }
        }

        PlaySound(swapped ? _confirmSound : _cancelSound);
        Close();
    }

    private void Close()
    {
        _isOpen = false;
        _root.Visible = false;
        GetTree().Paused = false;
    }

    private CombatActor? ResolvePlayer()
    {
        if (_simulation is null)
        {
            return null;
        }

        return _simulation.Actors.FirstOrDefault(actor => actor.IsPlayerControlled && actor.PlayerId == 1)
            ?? _simulation.Actors.FirstOrDefault(actor => actor.IsPlayerControlled);
    }

    private void BuildEntries()
    {
        _entries.Clear();
        if (_player is null)
        {
            return;
        }

        var currentId = _player.CurrentForm.Id;
        foreach (var formId in _player.FormArchive.ActiveLoadout)
        {
            var form = _player.FormArchive.GetForm(formId);
            if (form is null)
            {
                continue;
            }

            _entries.Add(new FormEntry(formId, form, formId == currentId));
        }
    }

    private void MoveCursor(int deltaX, int deltaY)
    {
        if (_entries.Count == 0)
        {
            return;
        }

        var columns = Mathf.Max(1, Columns);
        var target = _cursor;

        if (deltaX != 0)
        {
            target = Mathf.Clamp(_cursor + deltaX, 0, _entries.Count - 1);
        }
        else if (deltaY != 0)
        {
            var candidate = _cursor + (deltaY * columns);
            if (candidate >= 0 && candidate < _entries.Count)
            {
                target = candidate;
            }
        }

        if (target == _cursor)
        {
            return;
        }

        _cursor = target;
        _pulseTime = 0.0f;
        PlaySound(_moveSound);
        UpdateSelectionVisuals();
        UpdatePreview();
    }

    private void BuildInterface()
    {
        _root = new Control
        {
            Name = "FormSelectRoot",
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(_root);

        var dim = new ColorRect
        {
            Name = "Dim",
            Color = new Color(0.02f, 0.03f, 0.06f, 0.82f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        dim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _root.AddChild(dim);

        var margin = new MarginContainer();
        margin.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 56);
        margin.AddThemeConstantOverride("margin_top", 48);
        margin.AddThemeConstantOverride("margin_right", 56);
        margin.AddThemeConstantOverride("margin_bottom", 48);
        _root.AddChild(margin);

        var outer = new VBoxContainer();
        outer.AddThemeConstantOverride("separation", 18);
        margin.AddChild(outer);

        var title = MakeLabel("SELECT FORM", 40, Colors.White);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        outer.AddChild(title);

        var accentWrap = new CenterContainer();
        accentWrap.AddChild(new ColorRect
        {
            Color = new Color(0.30f, 0.80f, 1.00f, 0.55f),
            CustomMinimumSize = new Vector2(230, 3),
        });
        outer.AddChild(accentWrap);

        var body = new HBoxContainer();
        body.AddThemeConstantOverride("separation", 28);
        body.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        outer.AddChild(body);

        body.AddChild(BuildPreviewPanel());

        var gridWrap = new CenterContainer();
        gridWrap.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        gridWrap.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        body.AddChild(gridWrap);

        _grid = new GridContainer { Columns = Mathf.Max(1, Columns) };
        _grid.AddThemeConstantOverride("h_separation", 12);
        _grid.AddThemeConstantOverride("v_separation", 12);
        gridWrap.AddChild(_grid);

        var hint = MakeLabel(
            "Move: WASD     Confirm: J / Enter     Cancel: Q / Esc",
            15,
            new Color(0.72f, 0.77f, 0.88f));
        hint.HorizontalAlignment = HorizontalAlignment.Center;
        outer.AddChild(hint);
    }

    private Control BuildPreviewPanel()
    {
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(320, 0) };
        panel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;

        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.05f, 0.06f, 0.10f, 0.92f),
            BorderColor = new Color(0.30f, 0.85f, 1.00f, 0.55f),
        };
        style.SetBorderWidthAll(2);
        style.SetCornerRadiusAll(8);
        style.SetContentMarginAll(18);
        panel.AddThemeStyleboxOverride("panel", style);

        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 10);
        panel.AddChild(box);

        _previewPortrait = new TextureRect
        {
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            CustomMinimumSize = new Vector2(0, 240),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        box.AddChild(_previewPortrait);

        _previewName = MakeLabel("", 30, Colors.White);
        _previewName.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        box.AddChild(_previewName);

        _previewRole = MakeLabel("", 16, new Color(0.75f, 0.85f, 1.00f));
        box.AddChild(_previewRole);

        box.AddChild(new HSeparator());

        _previewStats = MakeLabel("", 16, new Color(0.85f, 0.95f, 0.85f));
        box.AddChild(_previewStats);

        _previewTags = MakeLabel("", 13, new Color(0.70f, 0.75f, 0.85f));
        _previewTags.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        box.AddChild(_previewTags);

        box.AddChild(new Control { SizeFlagsVertical = Control.SizeFlags.ExpandFill });
        return panel;
    }

    private void BuildGrid()
    {
        foreach (var child in _grid.GetChildren())
        {
            child.QueueFree();
        }

        _slotPanels.Clear();

        var columns = Mathf.Max(1, Columns);
        _grid.Columns = columns;
        var rows = Mathf.Max(Mathf.Max(1, Rows), Mathf.CeilToInt(_entries.Count / (float)columns));
        var capacity = columns * rows;

        for (var i = 0; i < capacity; i++)
        {
            _grid.AddChild(i < _entries.Count ? BuildFilledSlot(_entries[i]) : BuildEmptySlot());
        }
    }

    private PanelContainer BuildFilledSlot(FormEntry entry)
    {
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(168, 188) };

        var box = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        box.AddThemeConstantOverride("separation", 4);
        panel.AddChild(box);

        box.AddChild(new TextureRect
        {
            Texture = FormSelectPortraitResolver.Resolve(entry.Form, bust: true),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            CustomMinimumSize = new Vector2(120, 108),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        });

        var name = MakeLabel(entry.Form.DisplayName, 17, Colors.White);
        name.HorizontalAlignment = HorizontalAlignment.Center;
        name.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        box.AddChild(name);

        if (entry.IsCurrent)
        {
            box.AddChild(MakeBadge("EQUIPPED", new Color(1.00f, 0.78f, 0.32f)));
        }
        else
        {
            var role = MakeLabel(entry.Form.Role.ToString(), 11, new Color(0.66f, 0.76f, 0.92f));
            role.HorizontalAlignment = HorizontalAlignment.Center;
            box.AddChild(role);
        }

        _slotPanels.Add(panel);
        return panel;
    }

    private static PanelContainer BuildEmptySlot()
    {
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(168, 188) };
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.04f, 0.05f, 0.08f, 0.55f),
            BorderColor = new Color(0.18f, 0.20f, 0.26f, 0.70f),
        };
        style.SetBorderWidthAll(1);
        style.SetCornerRadiusAll(6);
        panel.AddThemeStyleboxOverride("panel", style);
        return panel;
    }

    private static Control MakeBadge(string text, Color color)
    {
        var wrap = new CenterContainer();
        var badge = new PanelContainer();

        var style = new StyleBoxFlat
        {
            BgColor = new Color(color.R, color.G, color.B, 0.20f),
            BorderColor = color,
        };
        style.SetBorderWidthAll(1);
        style.SetCornerRadiusAll(9);
        style.ContentMarginTop = 2;
        style.ContentMarginBottom = 2;
        style.ContentMarginLeft = 10;
        style.ContentMarginRight = 10;
        badge.AddThemeStyleboxOverride("panel", style);

        badge.AddChild(MakeLabel(text, 10, color));
        wrap.AddChild(badge);
        return wrap;
    }

    private void UpdateSelectionVisuals()
    {
        for (var i = 0; i < _slotPanels.Count; i++)
        {
            _slotPanels[i].AddThemeStyleboxOverride(
                "panel",
                SlotStyle(i == _cursor, _entries[i].IsCurrent, 0.0f));
        }
    }

    private static StyleBoxFlat SlotStyle(bool selected, bool isCurrent, float pulse)
    {
        var style = new StyleBoxFlat();
        if (selected)
        {
            var k = 0.5f + (0.5f * Mathf.Sin(pulse * 7.0f));
            style.BgColor = new Color(0.16f, 0.22f, 0.34f, 0.98f);
            style.BorderColor = new Color(0.30f, 0.78f, 1.00f).Lerp(new Color(0.66f, 0.95f, 1.00f), k);
        }
        else
        {
            style.BgColor = new Color(0.08f, 0.09f, 0.13f, 0.92f);
            style.BorderColor = isCurrent
                ? new Color(1.00f, 0.78f, 0.32f)
                : new Color(0.24f, 0.27f, 0.35f);
        }

        style.SetBorderWidthAll(selected ? 4 : 2);
        style.SetCornerRadiusAll(6);
        style.SetContentMarginAll(8);
        return style;
    }

    private void UpdatePreview()
    {
        if (_cursor < 0 || _cursor >= _entries.Count)
        {
            return;
        }

        var entry = _entries[_cursor];
        var form = entry.Form;

        _previewPortrait.Texture = FormSelectPortraitResolver.Resolve(form);
        _previewName.Text = form.DisplayName;
        _previewRole.Text = entry.IsCurrent
            ? $"{form.Role}  •  CURRENT FORM"
            : form.Role.ToString();
        _previewStats.Text =
            $"HP {form.MaxHealth}     Walk {form.WalkSpeed:0.0}     Dash {form.DashSpeed:0.0}";
        _previewTags.Text = form.RoleTags.Count > 0
            ? string.Join("   ", form.RoleTags.Select(tag => "#" + tag))
            : "";
    }

    private void BuildAudio()
    {
        _sfx = new AudioStreamPlayer
        {
            Name = "FormSelectSfx",
            Bus = "SFX",
            ProcessMode = ProcessModeEnum.Always,
        };
        AddChild(_sfx);

        _openSound = ProceduralAudioFactory.CreateStinger(new[] { 440.0f, 660.0f }, 0.10f, 0.26f);
        _moveSound = ProceduralAudioFactory.CreateStinger(new[] { 620.0f }, 0.045f, 0.20f);
        _confirmSound = ProceduralAudioFactory.CreateStinger(new[] { 523.0f, 784.0f }, 0.12f, 0.32f);
        _cancelSound = ProceduralAudioFactory.CreateStinger(new[] { 392.0f, 262.0f }, 0.10f, 0.26f);
    }

    private void PlaySound(AudioStream? stream)
    {
        if (stream is null)
        {
            return;
        }

        _sfx.Stream = stream;
        _sfx.Play();
    }

    private static Label MakeLabel(string text, int fontSize, Color color)
    {
        var label = new Label { Text = text };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color);
        return label;
    }
}
