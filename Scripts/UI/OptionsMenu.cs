using System;
using Godot;
using ProjectMannequin.LocalInput;
using ProjectMannequin.Progression;
using ProjectMannequin.Settings;

namespace ProjectMannequin.UI;

/// <summary>
/// Player-facing options surface for audio, video, accessibility, and controls.
/// </summary>
/// <remarks>
/// Deliberately thin: all navigation, clamping, and formatting live in
/// <see cref="OptionsModel"/> so they are covered by the deterministic suite.
/// This node renders rows and forwards input. Opens above the pause menu and
/// runs while the tree is paused, because it must be reachable mid-run.
/// </remarks>
public partial class OptionsMenu : CanvasLayer
{
    private const int RowHeight = 28;

    private Control _root = null!;
    private VBoxContainer _rowContainer = null!;
    private Label _descriptionLabel = null!;
    private OptionsModel _model = null!;
    private bool _open;

    public bool IsOpen => _open;

    /// <summary>Raised after the surface closes and edits are committed.</summary>
    public event Action? Closed;

    public override void _Ready()
    {
        // Above the pause menu at 30 so options can be opened from it.
        Layer = 40;
        ProcessMode = ProcessModeEnum.Always;
        _model = new OptionsModel(SettingsStore.Current.Clone());
        BuildUi();
        Close();
    }

    public void Open()
    {
        _model = new OptionsModel(SettingsStore.Current.Clone());
        _open = true;
        _root.Visible = true;

        // Drop focus from whatever menu button opened us, otherwise that
        // button keeps consuming Accept from behind the shade.
        GetViewport().GuiReleaseFocus();
        Refresh();
    }

    public void Close()
    {
        var wasOpen = _open;
        if (wasOpen)
        {
            Commit();
        }

        _open = false;
        _root.Visible = false;

        if (wasOpen)
        {
            Closed?.Invoke();
        }
    }

    // _Input, not _UnhandledInput: focused Buttons on the menu underneath
    // consume Accept during GUI input, which runs first. While this surface is
    // up it owns every button, so it intercepts ahead of the control layer.
    public override void _Input(InputEvent inputEvent)
    {
        if (!_open)
        {
            return;
        }

        if (UiInputRouter.IsPressed(inputEvent, LogicalUiAction.Cancel))
        {
            Close();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (UiInputRouter.IsPressed(inputEvent, LogicalUiAction.Up))
        {
            _model.MoveSelection(-1);
            Refresh();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (UiInputRouter.IsPressed(inputEvent, LogicalUiAction.Down))
        {
            _model.MoveSelection(1);
            Refresh();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (UiInputRouter.IsPressed(inputEvent, LogicalUiAction.Left))
        {
            _model.Adjust(-1);
            ApplyLive();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (UiInputRouter.IsPressed(inputEvent, LogicalUiAction.Right))
        {
            _model.Adjust(1);
            ApplyLive();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (UiInputRouter.IsPressed(inputEvent, LogicalUiAction.Accept))
        {
            if (_model.Activate())
            {
                Close();
            }
            else
            {
                ApplyLive();
            }

            GetViewport().SetInputAsHandled();
        }
    }

    /// <summary>
    /// Applies edits immediately so the player hears and sees the result while
    /// still on the row, rather than having to confirm and reopen.
    /// </summary>
    private void ApplyLive()
    {
        SettingsStore.ApplyAudio(_model.Settings);
        Refresh();
    }

    private void Commit()
    {
        if (_model.EraseRequested)
        {
            // Erase before writing settings back, otherwise Save would
            // immediately recreate the file the player just deleted.
            var removed = SaveDataReset.EraseAll();
            _model.ClearEraseRequest();
            GD.Print($"[Options] Erased {removed} save file(s) at the player's request.");
        }

        if (!_model.IsDirty)
        {
            return;
        }

        SettingsStore.Apply(_model.Settings);
        SettingsStore.Save(_model.Settings);
        _model.ClearDirty();
    }

    private void BuildUi()
    {
        _root = new Control
        {
            Name = "OptionsRoot",
            // Stop, so clicks never fall through to the menu behind us.
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        _root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(_root);

        var shade = new ColorRect
        {
            Name = "Shade",
            Color = new Color(0.02f, 0.03f, 0.06f, 0.86f),
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        shade.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _root.AddChild(shade);

        // A CenterContainer, not a Center anchor preset: the panel is sized by
        // its content, and the preset resolves offsets before that content
        // exists, which pushes the panel off screen and clips the value column.
        var centerer = new CenterContainer
        {
            Name = "OptionsCenterer",
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        centerer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _root.AddChild(centerer);

        var panel = new PanelContainer
        {
            Name = "OptionsPanel",
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        // Width only. Height follows the rows so the panel can never overrun
        // the viewport at the resolutions this surface is used at.
        panel.CustomMinimumSize = new Vector2(720, 0);
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.06f, 0.08f, 0.13f, 0.97f),
            BorderColor = new Color(0.55f, 0.78f, 0.95f, 0.85f),
            BorderWidthTop = 2,
            BorderWidthBottom = 2,
            BorderWidthLeft = 2,
            BorderWidthRight = 2,
            ContentMarginTop = 16,
            ContentMarginBottom = 16,
            ContentMarginLeft = 24,
            ContentMarginRight = 24,
        });
        centerer.AddChild(panel);

        var column = new VBoxContainer { Name = "Column" };
        column.AddThemeConstantOverride("separation", 8);
        panel.AddChild(column);

        var title = new Label
        {
            Name = "Title",
            Text = "OPTIONS",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        title.AddThemeFontSizeOverride("font_size", 26);
        title.AddThemeColorOverride("font_color", new Color(0.92f, 0.97f, 1.0f));
        column.AddChild(title);

        _rowContainer = new VBoxContainer { Name = "Rows" };
        _rowContainer.AddThemeConstantOverride("separation", 2);
        column.AddChild(_rowContainer);

        _descriptionLabel = new Label
        {
            Name = "Description",
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        _descriptionLabel.AddThemeFontSizeOverride("font_size", 15);
        _descriptionLabel.AddThemeColorOverride(
            "font_color",
            new Color(0.70f, 0.80f, 0.92f));
        _descriptionLabel.CustomMinimumSize = new Vector2(0, 40);
        column.AddChild(_descriptionLabel);

        var hint = new Label
        {
            Name = "Hint",
            Text = "Up/Down select    Left/Right adjust    Accept toggle    Cancel back",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        hint.AddThemeFontSizeOverride("font_size", 14);
        hint.AddThemeColorOverride("font_color", new Color(0.55f, 0.65f, 0.78f));
        column.AddChild(hint);
    }

    private void Refresh()
    {
        foreach (var child in _rowContainer.GetChildren())
        {
            child.QueueFree();
        }

        var rows = _model.BuildRows();
        for (var index = 0; index < rows.Count; index++)
        {
            var row = rows[index];
            var selected = index == _model.SelectedIndex;
            var line = new PanelContainer
            {
                Name = $"Row{index:00}",
                CustomMinimumSize = new Vector2(0, RowHeight),
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            if (selected)
            {
                line.AddThemeStyleboxOverride("panel", new StyleBoxFlat
                {
                    BgColor = new Color(0.16f, 0.32f, 0.48f, 0.92f),
                    ContentMarginLeft = 10,
                    ContentMarginRight = 10,
                });
            }

            var lineContent = new HBoxContainer();
            line.AddChild(lineContent);

            var label = new Label
            {
                Text = row.Label,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                VerticalAlignment = VerticalAlignment.Center,
            };
            label.AddThemeFontSizeOverride("font_size", 17);
            label.AddThemeColorOverride(
                "font_color",
                selected
                    ? new Color(1.0f, 1.0f, 1.0f)
                    : new Color(0.78f, 0.85f, 0.93f));
            lineContent.AddChild(label);

            var value = new Label
            {
                Text = row.ValueText,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                CustomMinimumSize = new Vector2(200, 0),
            };
            value.AddThemeFontSizeOverride("font_size", 17);
            value.AddThemeColorOverride(
                "font_color",
                selected
                    ? new Color(0.85f, 0.96f, 1.0f)
                    : new Color(0.62f, 0.72f, 0.84f));
            lineContent.AddChild(value);

            _rowContainer.AddChild(line);
        }

        _descriptionLabel.Text = _model.BuildRow(_model.SelectedId).Description;
    }
}
