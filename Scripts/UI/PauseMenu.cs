using System.Collections.Generic;
using System.Linq;
using Godot;
using ProjectMannequin.Combat;
using ProjectMannequin.Core;
using ProjectMannequin.Data;
using ProjectMannequin.LocalInput;
using ProjectMannequin.Presentation;
using ProjectMannequin.Progression;

namespace ProjectMannequin.UI;

public partial class PauseMenu : CanvasLayer
{
    [Export] public string MainMenuScenePath { get; set; } = "res://Scenes/UI/MainMenu.tscn";
    [Export] public NodePath SimulationPath { get; set; } = "../GameSimulation";
    [Export] public string PausePlatePath { get; set; } =
        "res://Assets/UI/Pause/project_mannequin_pause_plate_style_v1.png";

    private static readonly string[] MoveListCategoryOrder =
    {
        "Standing Normals",
        "Crouching Normals",
        "Jumping Normals",
        "Command Normals",
        "Throws",
        "Special Moves",
        "Super Arts",
    };

    private Control _panel = null!;
    private PanelContainer _loadoutPanel = null!;
    private GridContainer _loadoutGrid = null!;
    private Label _synergyLabel = null!;
    private PanelContainer _formLoadoutPanel = null!;
    private GridContainer _formLoadoutGrid = null!;
    private Label _formLoadoutHint = null!;
    private PanelContainer _moveListPanel = null!;
    private readonly System.Collections.Generic.List<Label> _legendLabels = new();
    private Label _moveListTitle = null!;
    private VBoxContainer _moveListContent = null!;
    private PanelContainer _artifactsPanel = null!;
    private VBoxContainer _artifactsList = null!;
    private Label _artifactsSummaryLabel = null!;
    private Button _resumeButton = null!;
    private Button _quitButton = null!;
    private OptionsMenu _optionsMenu = null!;
    private Control _pausePlateRoot = null!;
    private Control _pauseRecordContent = null!;
    private TextureRect _pausePortrait = null!;
    private Label _pauseFormLabel = null!;
    private Label _pauseRecordLabel = null!;
    private bool _paused;

    public override void _Ready()
    {
        // Pause must remain above stage cards, boss cinematics, and form select.
        Layer = 30;
        ProcessMode = ProcessModeEnum.Always;
        BuildInterface();

        _optionsMenu = new OptionsMenu { Name = "PauseOptionsMenu" };
        _optionsMenu.Closed += OnOptionsClosed;
        AddChild(_optionsMenu);

        SetPaused(false);
    }

    /// <summary>
    /// Hands the screen to the options surface. The tree stays paused and the
    /// pause plate hides so the two panels never stack or share focus.
    /// </summary>
    private void OpenOptions()
    {
        _panel.Visible = false;
        _optionsMenu.Open();
    }

    private void OnOptionsClosed()
    {
        if (!_paused)
        {
            return;
        }

        _panel.Visible = true;
        _resumeButton.CallDeferred("grab_focus");
    }

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        // The options surface owns every button while it is up, including
        // Cancel, so pause must not resume combat underneath it.
        if (_optionsMenu.IsOpen)
        {
            return;
        }

        // Escape is both Pause and Cancel on keyboard. Handle nested panels
        // first so Escape returns to the pause root instead of resuming combat.
        if (_paused && UiInputRouter.IsPressed(inputEvent, LogicalUiAction.Cancel))
        {
            if (_loadoutPanel.Visible)
            {
                ToggleLoadout(false);
            }
            else if (_formLoadoutPanel.Visible)
            {
                ToggleFormLoadout(false);
            }
            else if (_moveListPanel.Visible)
            {
                ToggleMoveList(false);
            }
            else if (_artifactsPanel.Visible)
            {
                ToggleArtifacts(false);
            }
            else
            {
                SetPaused(false);
            }

            GetViewport().SetInputAsHandled();
            return;
        }

        if (UiInputRouter.IsPressed(inputEvent, LogicalUiAction.Pause))
        {
            SetPaused(!_paused);
            GetViewport().SetInputAsHandled();
        }
    }

    private void BuildInterface()
    {
        _panel = new Control
        {
            Name = "PausePanel",
        };
        _panel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(_panel);

        var dim = new ColorRect
        {
            Name = "Dim",
            Color = new Color(0.0f, 0.0f, 0.0f, 0.62f),
        };
        dim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _panel.AddChild(dim);

        _pausePlateRoot = new Control
        {
            Name = "PausePlateRoot",
            CustomMinimumSize = new Vector2(1010.0f, 678.0f),
            Size = new Vector2(1010.0f, 678.0f),
        };
        _panel.AddChild(_pausePlateRoot);

        if (ResourceLoader.Exists(PausePlatePath))
        {
            var plate = new TextureRect
            {
                Name = "PausePlate",
                Texture = GD.Load<Texture2D>(PausePlatePath),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.Scale,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            plate.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            _pausePlateRoot.AddChild(plate);
        }

        var title = new Label
        {
            Text = "PAUSED",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Position = new Vector2(110.0f, 62.0f),
            Size = new Vector2(235.0f, 88.0f),
        };
        title.AddThemeFontSizeOverride("font_size", 27);
        title.AddThemeColorOverride("font_color", new Color(0.08f, 0.06f, 0.11f));
        _pausePlateRoot.AddChild(title);

        var box = new VBoxContainer
        {
            Name = "PauseButtons",
            Position = new Vector2(148.0f, 214.0f),
            Size = new Vector2(160.0f, 352.0f),
        };
        box.AddThemeConstantOverride("separation", 5);
        _pausePlateRoot.AddChild(box);

        _resumeButton = MakeButton("Resume");
        _resumeButton.Pressed += () => SetPaused(false);
        box.AddChild(_resumeButton);

        var moveList = MakeButton("Move List");
        moveList.Pressed += () => ToggleMoveList(true);
        box.AddChild(moveList);

        var loadout = MakeButton("Move Cards");
        loadout.Pressed += () => ToggleLoadout(true);
        box.AddChild(loadout);

        var formLoadout = MakeButton("Form Loadout");
        formLoadout.Pressed += () => ToggleFormLoadout(true);
        box.AddChild(formLoadout);

        var artifacts = MakeButton("Artifacts");
        artifacts.Pressed += () => ToggleArtifacts(true);
        box.AddChild(artifacts);

        var options = MakeButton("Options");
        options.Pressed += OpenOptions;
        box.AddChild(options);

        var restart = MakeButton("Restart Stage");
        restart.Pressed += RestartScene;
        box.AddChild(restart);

        var mainMenu = MakeButton("Archive Map");
        mainMenu.Pressed += ReturnToMainMenu;
        box.AddChild(mainMenu);

        _quitButton = MakeButton("Quit");
        _quitButton.Position = new Vector2(754.0f, 591.0f);
        _quitButton.Size = new Vector2(150.0f, 38.0f);
        _quitButton.CustomMinimumSize = new Vector2(150.0f, 38.0f);
        _quitButton.Pressed += () => GetTree().Quit();
        _pausePlateRoot.AddChild(_quitButton);

        BuildPauseRecord();
        PositionPausePlate();

        BuildLoadoutPanel();
        BuildFormLoadoutPanel();
        BuildMoveListPanel();
        BuildArtifactsPanel();
    }

    private void BuildPauseRecord()
    {
        _pauseRecordContent = new HBoxContainer
        {
            Name = "PauseRecordContent",
            Position = new Vector2(420.0f, 174.0f),
            Size = new Vector2(470.0f, 350.0f),
        };
        _pauseRecordContent.AddThemeConstantOverride("separation", 18);
        _pausePlateRoot.AddChild(_pauseRecordContent);

        _pausePortrait = new TextureRect
        {
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            CustomMinimumSize = new Vector2(190.0f, 320.0f),
        };
        _pauseRecordContent.AddChild(_pausePortrait);

        var record = new VBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        record.AddThemeConstantOverride("separation", 9);
        _pauseRecordContent.AddChild(record);

        var eyebrow = new Label
        {
            Text = "ACTIVE ARCHIVE RECORD",
            Modulate = new Color(0.42f, 0.94f, 0.92f),
        };
        eyebrow.AddThemeFontSizeOverride("font_size", 13);
        record.AddChild(eyebrow);

        _pauseFormLabel = new Label
        {
            Text = "BLANK MANNEQUIN",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        _pauseFormLabel.AddThemeFontSizeOverride("font_size", 27);
        record.AddChild(_pauseFormLabel);
        record.AddChild(new HSeparator());

        _pauseRecordLabel = new Label
        {
            Text = "",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            Modulate = new Color(0.82f, 0.88f, 0.94f),
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        _pauseRecordLabel.AddThemeFontSizeOverride("font_size", 15);
        record.AddChild(_pauseRecordLabel);
    }

    private void RefreshPauseRecord()
    {
        var player = GetPlayerActor();
        if (player is null)
        {
            _pausePortrait.Texture = null;
            _pauseFormLabel.Text = "NO ACTIVE FIGHTER";
            _pauseRecordLabel.Text = "Return to the Archive Map to deploy a fighter.";
            return;
        }

        _pausePortrait.Texture = FormSelectPortraitResolver.Resolve(player.CurrentForm);
        _pauseFormLabel.Text = player.CurrentForm.DisplayName.ToUpperInvariant();
        var session = RunSessionManager.Instance;
        _pauseRecordLabel.Text =
            $"HEALTH  {player.Health}/{player.CurrentForm.MaxHealth}\n"
            + $"METER   {player.Meter}/{player.CurrentForm.MaxMeter}\n"
            + $"FORMS   {player.FormArchive.ActiveLoadout.Count}/{player.FormArchive.ActiveFormLimit}\n"
            + $"CARDS   {session.EquippedMoveCards.Count}\n\n"
            + $"LIVES   {session.RemainingLives}\n"
            + $"RUN SCORE  {session.RunScore:000000}";
    }

    private void PositionPausePlate()
    {
        var viewportSize = GetViewport().GetVisibleRect().Size;
        var scale = Mathf.Clamp(
            Mathf.Min(viewportSize.X / 1280.0f, viewportSize.Y / 720.0f),
            0.85f,
            1.35f);
        _pausePlateRoot.Scale = Vector2.One * scale;
        var scaledSize = _pausePlateRoot.Size * scale;
        _pausePlateRoot.Position = (viewportSize - scaledSize) * 0.5f;
    }

    private void BuildLoadoutPanel()
    {
        _loadoutPanel = new PanelContainer { Name = "LoadoutPanel", Visible = false };
        ConfigureNestedPanel(_loadoutPanel);
        _pausePlateRoot.AddChild(_loadoutPanel);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_top", 18);
        margin.AddThemeConstantOverride("margin_left", 24);
        margin.AddThemeConstantOverride("margin_right", 24);
        margin.AddThemeConstantOverride("margin_bottom", 18);
        _loadoutPanel.AddChild(margin);

        var loadoutBox = new VBoxContainer();
        loadoutBox.AddThemeConstantOverride("separation", 12);
        margin.AddChild(loadoutBox);

        var loadoutTitle = new Label
        {
            Text = "MOVE CARD GRID",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        loadoutTitle.AddThemeFontSizeOverride("font_size", 28);
        loadoutBox.AddChild(loadoutTitle);

        _synergyLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            Modulate = new Color(1.0f, 0.9f, 0.6f),
        };
        _synergyLabel.AddThemeFontSizeOverride("font_size", 16);
        loadoutBox.AddChild(_synergyLabel);

        _loadoutGrid = new GridContainer { Columns = 3 };
        _loadoutGrid.AddThemeConstantOverride("h_separation", 10);
        _loadoutGrid.AddThemeConstantOverride("v_separation", 10);
        _loadoutGrid.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        loadoutBox.AddChild(_loadoutGrid);

        var closeLoadout = MakeButton("Close");
        closeLoadout.SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd;
        closeLoadout.Pressed += () => ToggleLoadout(false);
        loadoutBox.AddChild(closeLoadout);
    }

    private void ToggleLoadout(bool show)
    {
        _loadoutPanel.Visible = show;
        _pauseRecordContent.Visible = !show;
        _quitButton.Visible = !show;
        if (show)
        {
            _formLoadoutPanel.Visible = false;
            _moveListPanel.Visible = false;
            _artifactsPanel.Visible = false;
            RefreshLoadout();
            FocusFirstButton(_loadoutPanel);
        }
        else if (_paused)
        {
            _resumeButton.CallDeferred("grab_focus");
        }
    }

    private void RefreshLoadout()
    {
        foreach (var child in _loadoutGrid.GetChildren())
        {
            child.QueueFree();
        }

        var cards = RunSessionManager.Instance.EquippedMoveCards;
        if (cards.Count == 0)
        {
            _synergyLabel.Text = "No move cards yet — clear encounters to earn them.";
            return;
        }

        var names = RunSessionManager.Instance.GetActiveSynergyNames();
        var bonus = RunSessionManager.Instance.GetSynergyDamageBonusPercent();
        _synergyLabel.Text = names.Count > 0
            ? "Synergies: " + string.Join(", ", names) + $"  (+{bonus}% damage)"
            : "No active synergies — rearrange cards so related types sit adjacent.";

        for (var i = 0; i < cards.Count; i++)
        {
            _loadoutGrid.AddChild(BuildLoadoutCell(i, MoveCardCatalog.GetCard(cards[i])));
        }
    }

    private Control BuildLoadoutCell(int index, MoveCardData? card)
    {
        var cell = new VBoxContainer { CustomMinimumSize = new Vector2(150, 118) };
        cell.AddThemeConstantOverride("separation", 2);

        var icon = new TextureRect
        {
            Texture = ProceduralRewardIconFactory.GetIcon(card?.CardType.ToString() ?? "Basic"),
            CustomMinimumSize = new Vector2(48, 48),
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
        };
        cell.AddChild(icon);

        var name = new Label
        {
            Text = card?.DisplayName ?? "?",
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        name.AddThemeFontSizeOverride("font_size", 14);
        cell.AddChild(name);

        var typeLabel = new Label
        {
            Text = card?.CardType.ToString() ?? "",
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate = new Color(0.7f, 0.8f, 0.9f),
        };
        typeLabel.AddThemeFontSizeOverride("font_size", 11);
        cell.AddChild(typeLabel);

        var buttons = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        var left = new Button { Text = "◀", CustomMinimumSize = new Vector2(40, 26) };
        left.Pressed += () => MoveCard(index, -1);
        buttons.AddChild(left);
        var right = new Button { Text = "▶", CustomMinimumSize = new Vector2(40, 26) };
        right.Pressed += () => MoveCard(index, 1);
        buttons.AddChild(right);
        cell.AddChild(buttons);

        return cell;
    }

    private void MoveCard(int index, int direction)
    {
        var cards = RunSessionManager.Instance.EquippedMoveCards;
        var target = index + direction;
        if (index < 0 || index >= cards.Count || target < 0 || target >= cards.Count)
        {
            return;
        }

        (cards[index], cards[target]) = (cards[target], cards[index]);
        RefreshLoadout();
    }

    private void BuildMoveListPanel()
    {
        _moveListPanel = new PanelContainer { Name = "MoveListPanel", Visible = false };
        ConfigureNestedPanel(_moveListPanel);
        _pausePlateRoot.AddChild(_moveListPanel);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_top", 8);
        margin.AddThemeConstantOverride("margin_left", 12);
        margin.AddThemeConstantOverride("margin_right", 30);
        margin.AddThemeConstantOverride("margin_bottom", 8);
        _moveListPanel.AddChild(margin);

        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 6);
        margin.AddChild(box);

        _moveListTitle = new Label
        {
            Text = "MOVE LIST",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        _moveListTitle.AddThemeFontSizeOverride("font_size", 22);
        box.AddChild(_moveListTitle);

        // Kept as fields: legends name real controls, so they have to be
        // rewritten when the player switches between keyboard and a pad.
        _legendLabels.Clear();
        for (var line = 0; line < 3; line++)
        {
            var legendLabel = new Label
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                Modulate = new Color(0.75f, 0.85f, 0.95f),
            };
            legendLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            legendLabel.AddThemeFontSizeOverride("font_size", 11);
            box.AddChild(legendLabel);
            _legendLabels.Add(legendLabel);
        }

        RefreshLegends();

        box.AddChild(new HSeparator());

        var scroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(420, 270),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        scroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        box.AddChild(scroll);

        _moveListContent = new VBoxContainer();
        _moveListContent.AddThemeConstantOverride("separation", 3);
        _moveListContent.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        scroll.AddChild(_moveListContent);

        var close = MakeButton("Close");
        close.SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd;
        close.Pressed += () => ToggleMoveList(false);
        box.AddChild(close);
    }

    private void ToggleMoveList(bool show)
    {
        _moveListPanel.Visible = show;
        _pauseRecordContent.Visible = !show;
        _quitButton.Visible = !show;
        if (show)
        {
            _formLoadoutPanel.Visible = false;
            _loadoutPanel.Visible = false;
            _artifactsPanel.Visible = false;
            RefreshMoveList();
            FocusFirstButton(_moveListPanel);
        }
        else if (_paused)
        {
            _resumeButton.CallDeferred("grab_focus");
        }
    }

    private void RefreshLegends()
    {
        if (_legendLabels.Count < 3)
        {
            return;
        }

        _legendLabels[0].Text = InputCommandFormatter.DirectionLegend();
        _legendLabels[1].Text = InputCommandFormatter.ButtonLegend();
        _legendLabels[2].Text = InputCommandFormatter.SystemLegend();
    }

    private void RefreshMoveList()
    {
        // Re-resolve the active device first: the player may have plugged in or
        // unplugged a pad since this panel was last opened.
        InputGlyphs.Invalidate();
        RefreshLegends();
        foreach (var child in _moveListContent.GetChildren())
        {
            child.QueueFree();
        }

        var form = GetPlayerForm();
        if (form is null || form.Moves.Count == 0)
        {
            _moveListTitle.Text = "MOVE LIST";
            _moveListContent.AddChild(new Label
            {
                Text = "No active fighter yet — start a match to view its move list.",
                HorizontalAlignment = HorizontalAlignment.Center,
            });
            return;
        }

        _moveListTitle.Text = $"MOVE LIST — {form.DisplayName}";

        var grouped = new Dictionary<string, List<MoveData>>();
        var seen = new HashSet<string>();
        foreach (var move in form.Moves)
        {
            if (string.IsNullOrWhiteSpace(move.InputCommand))
            {
                continue;
            }

            var category = CategoryOf(move);
            var input = InputCommandFormatter.ToDisplayCommand(move.InputCommand);
            if (!seen.Add(category + "|" + input))
            {
                // Skip duplicate-input variants (for example close normals that
                // share a button with their far version).
                continue;
            }

            if (!grouped.TryGetValue(category, out var list))
            {
                list = new List<MoveData>();
                grouped[category] = list;
            }

            list.Add(move);
        }

        foreach (var category in MoveListCategoryOrder)
        {
            if (!grouped.TryGetValue(category, out var moves) || moves.Count == 0)
            {
                continue;
            }

            _moveListContent.AddChild(BuildCategoryHeader(category));
            foreach (var move in moves)
            {
                _moveListContent.AddChild(BuildMoveRow(move));
            }
        }
    }

    private static Control BuildCategoryHeader(string title)
    {
        var label = new Label
        {
            Text = title,
            Modulate = new Color(1.0f, 0.85f, 0.4f),
        };
        label.AddThemeFontSizeOverride("font_size", 17);
        return label;
    }

    private static Control BuildMoveRow(MoveData move)
    {
        var row = new HBoxContainer();
        row.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

        var name = new Label
        {
            Text = "   " + move.DisplayName,
            CustomMinimumSize = new Vector2(215, 0),
        };
        name.AddThemeFontSizeOverride("font_size", 15);
        name.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AddChild(name);

        var input = new Label
        {
            Text = InputCommandFormatter.ToDisplayCommand(move.InputCommand),
            HorizontalAlignment = HorizontalAlignment.Right,
            CustomMinimumSize = new Vector2(115, 0),
            Modulate = new Color(0.82f, 1.0f, 0.82f),
        };
        input.AddThemeFontSizeOverride("font_size", 15);
        row.AddChild(input);

        return row;
    }

    private static string CategoryOf(MoveData move)
    {
        if (move.IsSuper || move.Tags.Contains("super"))
        {
            return "Super Arts";
        }

        if (move.Tags.Contains("throw") || move.AttackHeight == AttackHeight.Throw)
        {
            return "Throws";
        }

        if (move.Tags.Contains("special"))
        {
            return "Special Moves";
        }

        if (move.Posture == MovePosture.Air || (move.AllowAir && !move.AllowGround))
        {
            return "Jumping Normals";
        }

        if (move.Posture == MovePosture.Crouching)
        {
            return "Crouching Normals";
        }

        return move.InputCommand.Any(char.IsDigit)
            ? "Command Normals"
            : "Standing Normals";
    }

    private void BuildFormLoadoutPanel()
    {
        _formLoadoutPanel = new PanelContainer { Name = "FormLoadoutPanel", Visible = false };
        ConfigureNestedPanel(_formLoadoutPanel);
        _pausePlateRoot.AddChild(_formLoadoutPanel);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_top", 18);
        margin.AddThemeConstantOverride("margin_left", 24);
        margin.AddThemeConstantOverride("margin_right", 24);
        margin.AddThemeConstantOverride("margin_bottom", 18);
        _formLoadoutPanel.AddChild(margin);

        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 12);
        margin.AddChild(box);

        var title = new Label
        {
            Text = "FORM LOADOUT",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        title.AddThemeFontSizeOverride("font_size", 28);
        box.AddChild(title);

        _formLoadoutHint = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            Modulate = new Color(0.75f, 0.85f, 1.0f),
        };
        _formLoadoutHint.AddThemeFontSizeOverride("font_size", 15);
        box.AddChild(_formLoadoutHint);

        _formLoadoutGrid = new GridContainer { Columns = 3 };
        _formLoadoutGrid.AddThemeConstantOverride("h_separation", 12);
        _formLoadoutGrid.AddThemeConstantOverride("v_separation", 12);
        _formLoadoutGrid.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        box.AddChild(_formLoadoutGrid);

        var close = MakeButton("Close");
        close.SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd;
        close.Pressed += () => ToggleFormLoadout(false);
        box.AddChild(close);
    }

    private void ToggleFormLoadout(bool show)
    {
        _formLoadoutPanel.Visible = show;
        _pauseRecordContent.Visible = !show;
        _quitButton.Visible = !show;
        if (show)
        {
            _loadoutPanel.Visible = false;
            _moveListPanel.Visible = false;
            _artifactsPanel.Visible = false;
            RefreshFormLoadout();
            FocusFirstButton(_formLoadoutPanel);
        }
        else if (_paused)
        {
            _resumeButton.CallDeferred("grab_focus");
        }
    }

    private void RefreshFormLoadout()
    {
        foreach (var child in _formLoadoutGrid.GetChildren())
        {
            child.QueueFree();
        }

        var player = GetPlayerActor();
        if (player is null || player.FormArchive.UnlockedForms.Count == 0)
        {
            _formLoadoutHint.Text = "No forms archived yet \u2014 defeat bosses to archive their forms.";
            return;
        }

        var archive = player.FormArchive;
        var limit = Mathf.Min(archive.ActiveFormLimit, GameConstants.MaxPlayers);
        _formLoadoutHint.Text =
            $"{archive.ActiveLoadout.Count}/{limit} equipped \u2014 equipped forms can be swapped to mid-fight with Q.";

        foreach (var form in archive.UnlockedForms.Values)
        {
            _formLoadoutGrid.AddChild(BuildFormLoadoutCell(player, form));
        }
    }

    private Control BuildFormLoadoutCell(CombatActor player, CharacterData form)
    {
        var archive = player.FormArchive;
        var equipped = archive.CanUse(form.Id);
        var isCurrent = form.Id == player.CurrentForm.Id;
        var limit = Mathf.Min(archive.ActiveFormLimit, GameConstants.MaxPlayers);

        var cell = new VBoxContainer { CustomMinimumSize = new Vector2(150, 196) };
        cell.AddThemeConstantOverride("separation", 4);

        cell.AddChild(new TextureRect
        {
            Texture = FormSelectPortraitResolver.Resolve(form, bust: true),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            CustomMinimumSize = new Vector2(120, 96),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        });

        var name = new Label
        {
            Text = form.DisplayName,
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        name.AddThemeFontSizeOverride("font_size", 15);
        cell.AddChild(name);

        var state = new Label
        {
            Text = equipped ? (isCurrent ? "EQUIPPED \u2022 ACTIVE" : "EQUIPPED") : "ARCHIVED",
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate = equipped ? new Color(0.55f, 1.0f, 0.70f) : new Color(0.70f, 0.75f, 0.85f),
        };
        state.AddThemeFontSizeOverride("font_size", 11);
        cell.AddChild(state);

        Button button;
        if (equipped)
        {
            button = MakeButton("Unequip");
            button.Disabled = isCurrent || archive.ActiveLoadout.Count <= 1;
            button.Pressed += () =>
            {
                player.FormArchive.Unequip(form.Id);
                RefreshFormLoadout();
            };
        }
        else
        {
            button = MakeButton("Equip");
            button.Disabled = archive.ActiveLoadout.Count >= limit;
            button.Pressed += () =>
            {
                player.FormArchive.TryEquip(form.Id);
                RefreshFormLoadout();
            };
        }

        button.CustomMinimumSize = new Vector2(0, 30);
        cell.AddChild(button);

        return cell;
    }

    private CombatActor? GetPlayerActor()
    {
        var simulation = GetNodeOrNull<GameSimulation>(SimulationPath);
        if (simulation is null)
        {
            return null;
        }

        return simulation.Actors.FirstOrDefault(actor => actor.IsPlayerControlled && actor.PlayerId == 1)
            ?? simulation.Actors.FirstOrDefault(actor => actor.IsPlayerControlled)
            ?? simulation.Actors.FirstOrDefault(actor => actor.TeamId == 1);
    }

    private CharacterData? GetPlayerForm()
    {
        return GetPlayerActor()?.CurrentForm;
    }

    private void BuildArtifactsPanel()
    {
        _artifactsPanel = new PanelContainer { Name = "ArtifactsPanel", Visible = false };
        ConfigureNestedPanel(_artifactsPanel);
        _pausePlateRoot.AddChild(_artifactsPanel);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_top", 18);
        margin.AddThemeConstantOverride("margin_left", 24);
        margin.AddThemeConstantOverride("margin_right", 24);
        margin.AddThemeConstantOverride("margin_bottom", 18);
        _artifactsPanel.AddChild(margin);

        var artifactsBox = new VBoxContainer();
        artifactsBox.AddThemeConstantOverride("separation", 10);
        margin.AddChild(artifactsBox);

        var artifactsTitle = new Label
        {
            Text = "ACTIVE ARTIFACTS",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        artifactsTitle.AddThemeFontSizeOverride("font_size", 28);
        artifactsBox.AddChild(artifactsTitle);

        _artifactsSummaryLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            Modulate = new Color(0.72f, 0.86f, 1.0f),
        };
        _artifactsSummaryLabel.AddThemeFontSizeOverride("font_size", 16);
        artifactsBox.AddChild(_artifactsSummaryLabel);

        var scroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(440, 270),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        artifactsBox.AddChild(scroll);

        _artifactsList = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _artifactsList.AddThemeConstantOverride("separation", 8);
        scroll.AddChild(_artifactsList);

        var close = MakeButton("Close");
        close.SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd;
        close.Pressed += () => ToggleArtifacts(false);
        artifactsBox.AddChild(close);
    }

    private void ToggleArtifacts(bool show)
    {
        _artifactsPanel.Visible = show;
        _pauseRecordContent.Visible = !show;
        _quitButton.Visible = !show;
        if (show)
        {
            _loadoutPanel.Visible = false;
            _formLoadoutPanel.Visible = false;
            _moveListPanel.Visible = false;
            RefreshArtifacts();
            FocusFirstButton(_artifactsPanel);
        }
        else if (_paused)
        {
            _resumeButton.CallDeferred("grab_focus");
        }
    }

    private void RefreshArtifacts()
    {
        foreach (var child in _artifactsList.GetChildren())
        {
            child.QueueFree();
        }

        var ids = RunSessionManager.Instance.ActiveArtifacts;
        if (ids.Count == 0)
        {
            _artifactsSummaryLabel.Text = "No artifacts yet \u2014 defeat bosses and draft them between stages.";
            _artifactsList.AddChild(new Label
            {
                Text = "Cursed artifacts trade a powerful edge for a real drawback. Choose your risk.",
                HorizontalAlignment = HorizontalAlignment.Center,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                Modulate = Colors.Gray,
            });
            return;
        }

        int totalDamage = 0, totalDefense = 0, totalMeter = 0, totalDrain = 0;
        foreach (var id in ids)
        {
            var artifact = ArtifactCatalog.GetArtifact(id);
            if (artifact is null)
            {
                continue;
            }

            totalDamage += artifact.DamageModifierPercent;
            totalDefense += artifact.DefenseModifierPercent;
            totalMeter += artifact.MeterGainModifierPercent;
            totalDrain += artifact.HealthDrainPerSecond;
            _artifactsList.AddChild(BuildArtifactRow(artifact));
        }

        var parts = new List<string>();
        if (totalDamage != 0) parts.Add($"{Signed(totalDamage)}% dmg");
        if (totalDefense != 0) parts.Add($"{Signed(totalDefense)}% def");
        if (totalMeter != 0) parts.Add($"{Signed(totalMeter)}% meter");
        if (totalDrain != 0) parts.Add($"-{totalDrain} hp/s drain");
        _artifactsSummaryLabel.Text = parts.Count > 0
            ? "Net effect:  " + string.Join("    ", parts)
            : "Net effect:  none";
    }

    private static Control BuildArtifactRow(ArtifactData artifact)
    {
        var row = new PanelContainer();
        var rowMargin = new MarginContainer();
        rowMargin.AddThemeConstantOverride("margin_left", 10);
        rowMargin.AddThemeConstantOverride("margin_right", 10);
        rowMargin.AddThemeConstantOverride("margin_top", 6);
        rowMargin.AddThemeConstantOverride("margin_bottom", 6);
        row.AddChild(rowMargin);

        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 2);
        rowMargin.AddChild(box);

        var nameLabel = new Label
        {
            Text = artifact.IsCursed
                ? $"\u2620 {artifact.DisplayName}   ({artifact.Rarity})"
                : $"{artifact.DisplayName}   ({artifact.Rarity})",
            Modulate = artifact.IsCursed ? new Color(1.0f, 0.55f, 0.5f) : new Color(1.0f, 0.92f, 0.66f),
        };
        nameLabel.AddThemeFontSizeOverride("font_size", 19);
        box.AddChild(nameLabel);

        var descLabel = new Label
        {
            Text = artifact.Description,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            Modulate = new Color(0.82f, 0.88f, 0.94f),
        };
        descLabel.AddThemeFontSizeOverride("font_size", 14);
        box.AddChild(descLabel);

        return row;
    }

    private static string Signed(int value)
    {
        return value >= 0 ? $"+{value}" : value.ToString();
    }

    private static void ConfigureNestedPanel(PanelContainer panel)
    {
        panel.Position = new Vector2(405.0f, 146.0f);
        panel.Size = new Vector2(480.0f, 420.0f);
        panel.CustomMinimumSize = new Vector2(480.0f, 420.0f);
        panel.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
    }

    private void SetPaused(bool paused)
    {
        _paused = paused;
        _panel.Visible = paused;
        if (!paused)
        {
            _loadoutPanel.Visible = false;
            _formLoadoutPanel.Visible = false;
            _moveListPanel.Visible = false;
            _artifactsPanel.Visible = false;
        }

        GetTree().Paused = paused;
        if (paused)
        {
            _pauseRecordContent.Visible = true;
            _quitButton.Visible = true;
            RefreshPauseRecord();
            PositionPausePlate();
            _resumeButton.CallDeferred("grab_focus");
        }
    }

    private void RestartScene()
    {
        SetPaused(false);
        RunSessionManager.Instance.RestoreStageCheckpoint();
        SceneFlowCoordinator.Reload(GetTree(), "RESTARTING STAGE");
    }

    private void ReturnToMainMenu()
    {
        SetPaused(false);
        RunSessionManager.Instance.RestoreStageCheckpoint();
        SceneFlowCoordinator.TransitionTo(
            GetTree(),
            MainMenuScenePath,
            "RETURNING TO ARCHIVE MAP");
    }

    private static Button MakeButton(string text)
    {
        var button = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(160, 42),
            FocusMode = Control.FocusModeEnum.All,
        };
        button.AddThemeFontSizeOverride("font_size", 13);
        button.AddThemeStyleboxOverride("normal", PauseButtonStyle(
            new Color(0.06f, 0.045f, 0.075f, 0.72f),
            new Color(0.30f, 0.56f, 0.62f, 0.52f),
            1));
        button.AddThemeStyleboxOverride("hover", PauseButtonStyle(
            new Color(0.10f, 0.07f, 0.13f, 0.92f),
            new Color(0.42f, 0.94f, 0.92f),
            2));
        button.AddThemeStyleboxOverride("focus", PauseButtonStyle(
            new Color(0.13f, 0.085f, 0.15f, 0.98f),
            new Color(1.0f, 0.78f, 0.28f),
            3));
        button.AddThemeStyleboxOverride("pressed", PauseButtonStyle(
            new Color(0.16f, 0.10f, 0.18f, 1.0f),
            new Color(1.0f, 0.48f, 0.32f),
            2));
        return button;
    }

    private static StyleBoxFlat PauseButtonStyle(Color background, Color border, int width)
    {
        return new StyleBoxFlat
        {
            BgColor = background,
            BorderColor = border,
            BorderWidthLeft = width,
            BorderWidthTop = width,
            BorderWidthRight = width,
            BorderWidthBottom = width,
            CornerRadiusTopLeft = 3,
            CornerRadiusTopRight = 3,
            CornerRadiusBottomLeft = 3,
            CornerRadiusBottomRight = 3,
            ContentMarginLeft = 8.0f,
            ContentMarginTop = 5.0f,
            ContentMarginRight = 8.0f,
            ContentMarginBottom = 5.0f,
        };
    }

    private static bool FocusFirstButton(Node root)
    {
        foreach (var child in root.GetChildren())
        {
            if (child is Button { Visible: true, Disabled: false } button)
            {
                button.CallDeferred("grab_focus");
                return true;
            }

            if (FocusFirstButton(child))
            {
                return true;
            }
        }

        return false;
    }
}
