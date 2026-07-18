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
    [Export] public string FormSelectFramePath { get; set; } =
        "res://Assets/UI/FormSelect/project_mannequin_form_select_frame_style_v1.png";

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
    private Control _frameRoot = null!;
    private GridContainer _grid = null!;
    private TextureRect _previewPortrait = null!;
    private Label _previewName = null!;
    private Label _previewRole = null!;
    private Label _previewStats = null!;
    private Label _previewTags = null!;
    private Label _loadoutStatus = null!;

    private int _cursor;
    private bool _isOpen;

    public bool IsOpen => _isOpen;
    public int EntryCount => _entries.Count;
    public string SelectedFormId => _cursor >= 0 && _cursor < _entries.Count
        ? _entries[_cursor].Id
        : "";
    public string PreviewName => _previewName?.Text ?? "";

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

        PositionFrame();

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
        _loadoutStatus.Text = $"ACTIVE LOADOUT  //  {_entries.Count} INHERITANCE RECORDS";
        PositionFrame();

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

        var currentIndex = _entries.FindIndex(entry => entry.IsCurrent);
        if (currentIndex >= 0 && _entries.Count > 1)
        {
            var current = _entries[currentIndex];
            _entries.RemoveAt(currentIndex);
            _entries.Insert(1, current);
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

        _frameRoot = new Control
        {
            Name = "FormSelectFrameRoot",
            CustomMinimumSize = new Vector2(1150.0f, 642.0f),
            Size = new Vector2(1150.0f, 642.0f),
        };
        _root.AddChild(_frameRoot);

        if (ResourceLoader.Exists(FormSelectFramePath))
        {
            var frame = new TextureRect
            {
                Name = "FormSelectFrame",
                Texture = GD.Load<Texture2D>(FormSelectFramePath),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.Scale,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            frame.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            _frameRoot.AddChild(frame);
        }

        var title = MakeLabel("SELECT FORM", 32, Colors.White);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.VerticalAlignment = VerticalAlignment.Center;
        title.Position = new Vector2(390.0f, 34.0f);
        title.Size = new Vector2(700.0f, 58.0f);
        _frameRoot.AddChild(title);

        var preview = BuildPreviewPanel();
        preview.Position = new Vector2(58.0f, 118.0f);
        preview.Size = new Vector2(330.0f, 470.0f);
        _frameRoot.AddChild(preview);

        var gridWrap = new CenterContainer();
        gridWrap.Position = new Vector2(430.0f, 144.0f);
        gridWrap.Size = new Vector2(660.0f, 390.0f);
        _frameRoot.AddChild(gridWrap);

        _grid = new GridContainer { Columns = Mathf.Max(1, Columns) };
        _grid.AddThemeConstantOverride("h_separation", 10);
        _grid.AddThemeConstantOverride("v_separation", 10);
        gridWrap.AddChild(_grid);

        _loadoutStatus = MakeLabel(
            "ACTIVE LOADOUT  //  INHERITANCE RECORDS",
            13,
            new Color(0.78f, 0.86f, 0.92f));
        _loadoutStatus.HorizontalAlignment = HorizontalAlignment.Center;
        _loadoutStatus.VerticalAlignment = VerticalAlignment.Center;
        _loadoutStatus.Position = new Vector2(430.0f, 570.0f);
        _loadoutStatus.Size = new Vector2(660.0f, 34.0f);
        _frameRoot.AddChild(_loadoutStatus);
        PositionFrame();
    }

    private Control BuildPreviewPanel()
    {
        var panel = new Control
        {
            CustomMinimumSize = new Vector2(330, 470),
            Size = new Vector2(330, 470),
        };

        _previewPortrait = new TextureRect
        {
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            Position = new Vector2(18.0f, 10.0f),
            Size = new Vector2(294.0f, 270.0f),
        };
        panel.AddChild(_previewPortrait);

        _previewName = MakeLabel("", 24, Colors.White);
        _previewName.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _previewName.Position = new Vector2(18.0f, 286.0f);
        _previewName.Size = new Vector2(294.0f, 42.0f);
        panel.AddChild(_previewName);

        _previewRole = MakeLabel("", 14, new Color(0.75f, 0.85f, 1.00f));
        _previewRole.Position = new Vector2(18.0f, 330.0f);
        _previewRole.Size = new Vector2(294.0f, 28.0f);
        panel.AddChild(_previewRole);

        _previewStats = MakeLabel("", 14, new Color(0.85f, 0.95f, 0.85f));
        _previewStats.Position = new Vector2(18.0f, 385.0f);
        _previewStats.Size = new Vector2(294.0f, 26.0f);
        panel.AddChild(_previewStats);

        _previewTags = MakeLabel("", 13, new Color(0.70f, 0.75f, 0.85f));
        _previewTags.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _previewTags.Position = new Vector2(18.0f, 416.0f);
        _previewTags.Size = new Vector2(294.0f, 48.0f);
        panel.AddChild(_previewTags);
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
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(190, 175) };

        var box = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        box.AddThemeConstantOverride("separation", 4);
        panel.AddChild(box);

        box.AddChild(new TextureRect
        {
            Texture = FormSelectPortraitResolver.Resolve(entry.Form, bust: true),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            CustomMinimumSize = new Vector2(120, 96),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        });

        var name = MakeLabel(entry.Form.DisplayName, 15, Colors.White);
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
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(190, 175) };
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.04f, 0.05f, 0.08f, 0.05f),
            BorderColor = new Color(0.18f, 0.20f, 0.26f, 0.20f),
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
            style.BgColor = new Color(0.16f, 0.22f, 0.34f, 0.20f);
            style.BorderColor = new Color(0.30f, 0.78f, 1.00f).Lerp(new Color(0.66f, 0.95f, 1.00f), k);
        }
        else
        {
            style.BgColor = new Color(0.08f, 0.09f, 0.13f, 0.04f);
            style.BorderColor = isCurrent
                ? new Color(1.00f, 0.78f, 0.32f)
                : new Color(0.24f, 0.27f, 0.35f, 0.24f);
        }

        style.SetBorderWidthAll(selected ? 4 : 2);
        style.SetCornerRadiusAll(2);
        style.SetContentMarginAll(6);
        return style;
    }

    private void PositionFrame()
    {
        if (_frameRoot is null)
        {
            return;
        }

        var viewportSize = GetViewport().GetVisibleRect().Size;
        var scale = Mathf.Clamp(
            Mathf.Min(viewportSize.X / 1280.0f, viewportSize.Y / 720.0f),
            0.85f,
            1.35f);
        _frameRoot.Scale = Vector2.One * scale;
        var scaledSize = _frameRoot.Size * scale;
        _frameRoot.Position = (viewportSize - scaledSize) * 0.5f;
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

    public override void _ExitTree()
    {
        if (_sfx is not null)
        {
            _sfx.Stop();
            _sfx.Stream = null;
        }

        _openSound?.Dispose();
        _moveSound?.Dispose();
        _confirmSound?.Dispose();
        _cancelSound?.Dispose();
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
