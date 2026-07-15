using System.Collections.Generic;
using Godot;
using ProjectMannequin.Core;
using ProjectMannequin.Data;
using ProjectMannequin.LocalInput;
using ProjectMannequin.Progression;

namespace ProjectMannequin.UI;

public partial class ArchiveHubScene : Control
{
    [Export] public string MainMenuScenePath { get; set; } = "res://Scenes/UI/MainMenu.tscn";

    private Label _titleLabel = null!;
    private VBoxContainer _trophyList = null!;
    private VBoxContainer _loreList = null!;
    private VBoxContainer _npcList = null!;
    private PanelContainer _dialogPanel = null!;
    private Label _dialogLabel = null!;
    private Button _backButton = null!;
    private Button _trainingButton = null!;
    private Button _dialogCloseButton = null!;
    private Control? _focusBeforeDialog;

    public override void _Ready()
    {
        // Setup basic UI
        var colorRect = new ColorRect
        {
            Color = new Color(0.1f, 0.1f, 0.2f),
        };
        colorRect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(colorRect);

        var marginContainer = new MarginContainer();
        marginContainer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        marginContainer.AddThemeConstantOverride("margin_top", 40);
        marginContainer.AddThemeConstantOverride("margin_left", 60);
        marginContainer.AddThemeConstantOverride("margin_right", 60);
        marginContainer.AddThemeConstantOverride("margin_bottom", 40);
        AddChild(marginContainer);

        var vbox = new VBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Begin,
        };
        marginContainer.AddChild(vbox);

        _titleLabel = new Label
        {
            Text = "The Form Archive (Hub)",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        _titleLabel.AddThemeFontSizeOverride("font_size", 48);
        vbox.AddChild(_titleLabel);

        var squadName = SquadNameGenerator.Generate(
            MvpProgressStore.Load().UnlockedFormIds);
        var squadLabel = new Label
        {
            Text = $"Squad: {squadName}",
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate = new Color(0.56f, 0.86f, 1.0f),
        };
        squadLabel.AddThemeFontSizeOverride("font_size", 22);
        vbox.AddChild(squadLabel);

        vbox.AddChild(new HSeparator { CustomMinimumSize = new Vector2(0, 20) });

        var subtitle = new Label
        {
            Text = "Unlocked Trophies:",
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        subtitle.AddThemeFontSizeOverride("font_size", 24);
        vbox.AddChild(subtitle);

        _trophyList = new VBoxContainer();
        vbox.AddChild(_trophyList);

        vbox.AddChild(new HSeparator { CustomMinimumSize = new Vector2(0, 16) });

        var loreSubtitle = new Label
        {
            Text = "Recovered Lore (Codex):",
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        loreSubtitle.AddThemeFontSizeOverride("font_size", 24);
        vbox.AddChild(loreSubtitle);

        _loreList = new VBoxContainer();
        vbox.AddChild(_loreList);

        var spacer = new Control { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        vbox.AddChild(spacer);

        // Training Room Button
        _trainingButton = new Button
        {
            Text = "Enter Training Room",
            CustomMinimumSize = new Vector2(300, 60),
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
        };
        _trainingButton.AddThemeFontSizeOverride("font_size", 24);
        _trainingButton.Pressed += StartTrainingRoom;
        vbox.AddChild(_trainingButton);
        
        vbox.AddChild(new HSeparator { CustomMinimumSize = new Vector2(0, 20) });
        
        // NPC Interactions
        var npcSubtitle = new Label
        {
            Text = "Archive Residents:",
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        npcSubtitle.AddThemeFontSizeOverride("font_size", 24);
        vbox.AddChild(npcSubtitle);
        
        _npcList = new VBoxContainer();
        vbox.AddChild(_npcList);
        PopulateNPCs();

        vbox.AddChild(new HSeparator { CustomMinimumSize = new Vector2(0, 20) });

        _backButton = new Button
        {
            Text = "Return to Main Menu",
            CustomMinimumSize = new Vector2(300, 60),
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
        };
        _backButton.AddThemeFontSizeOverride("font_size", 24);
        _backButton.Pressed += ReturnToMainMenu;
        vbox.AddChild(_backButton);

        PopulateTrophies();
        PopulateLore();
        
        // Dialog Popup Setup
        _dialogPanel = new PanelContainer
        {
            Visible = false,
        };
        _dialogPanel.SetAnchorsPreset(Control.LayoutPreset.Center);
        var dialogMargin = new MarginContainer();
        dialogMargin.AddThemeConstantOverride("margin_top", 20);
        dialogMargin.AddThemeConstantOverride("margin_left", 30);
        dialogMargin.AddThemeConstantOverride("margin_right", 30);
        dialogMargin.AddThemeConstantOverride("margin_bottom", 20);
        _dialogPanel.AddChild(dialogMargin);
        
        var dialogVBox = new VBoxContainer();
        dialogMargin.AddChild(dialogVBox);
        
        _dialogLabel = new Label
        {
            Text = "",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        _dialogLabel.AddThemeFontSizeOverride("font_size", 24);
        _dialogLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _dialogLabel.CustomMinimumSize = new Vector2(560, 0);
        dialogVBox.AddChild(_dialogLabel);
        
        _dialogCloseButton = new Button
        {
            Text = "Close",
            CustomMinimumSize = new Vector2(100, 40),
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
        };
        _dialogCloseButton.Pressed += CloseDialog;
        dialogVBox.AddChild(_dialogCloseButton);
        
        AddChild(_dialogPanel);
        _trainingButton.CallDeferred("grab_focus");
    }

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (!UiInputRouter.IsPressed(inputEvent, LogicalUiAction.Cancel))
        {
            return;
        }

        if (_dialogPanel.Visible)
        {
            CloseDialog();
        }
        else
        {
            ReturnToMainMenu();
        }

        GetViewport().SetInputAsHandled();
    }

    private void PopulateTrophies()
    {
        var progress = MvpProgressStore.Load();
        if (progress.UnlockedFormIds.Count == 0)
        {
            var emptyLabel = new Label
            {
                Text = "No forms archived yet. Complete worlds to collect forms!",
                Modulate = Colors.Gray,
            };
            _trophyList.AddChild(emptyLabel);
            return;
        }

        foreach (var formId in progress.UnlockedFormIds)
        {
            var label = new Label
            {
                Text = $"\U0001F3C6 {PrettyName(formId)}",
            };
            label.AddThemeFontSizeOverride("font_size", 20);
            _trophyList.AddChild(label);
        }
    }

    private void PopulateLore()
    {
        var progress = MvpProgressStore.Load();
        if (progress.UnlockedLoreFragments.Count == 0)
        {
            var emptyLabel = new Label
            {
                Text = "No echoes recovered yet. Defeat bosses to unlock the Archive's history.",
                Modulate = Colors.Gray,
            };
            _loreList.AddChild(emptyLabel);
            return;
        }

        foreach (var id in progress.UnlockedLoreFragments)
        {
            var fragment = LoreCatalog.GetFragment(id);
            if (fragment == null)
            {
                continue;
            }

            var button = new Button
            {
                Text = $"\U0001F4DC {fragment.Title}  \u2014  {fragment.World}",
                SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
            };
            button.AddThemeFontSizeOverride("font_size", 18);
            var captured = fragment;
            button.Pressed += () => ShowLoreFragment(captured);
            _loreList.AddChild(button);
        }
    }

    private void ShowLoreFragment(LoreFragment fragment)
    {
        ShowDialog(
            $"\u201c{fragment.Title}\u201d\n\u2014  {fragment.World}  \u2014\n\n{fragment.Body}");
    }

    private static string PrettyName(string formId)
    {
        if (string.IsNullOrWhiteSpace(formId))
        {
            return "Unknown Form";
        }

        var words = formId.Replace("_form", "").Split('_', System.StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < words.Length; i++)
        {
            words[i] = char.ToUpperInvariant(words[i][0]) + words[i][1..];
        }

        return string.Join(" ", words);
    }

    private void PopulateNPCs()
    {
        var progress = MvpProgressStore.Load();
        var loreCount = progress.UnlockedLoreFragments.Count;
        var formCount = progress.UnlockedFormIds.Count;

        var curatorButton = MakeNpcButton("Talk to The Curator");
        curatorButton.Pressed += () => ShowDialog(CuratorLine(formCount, loreCount));
        _npcList.AddChild(curatorButton);

        var blacksmithButton = MakeNpcButton("Talk to The Blacksmith");
        blacksmithButton.Pressed += () => ShowDialog(BlacksmithLine(formCount));
        _npcList.AddChild(blacksmithButton);

        var lorekeeperButton = MakeNpcButton("Talk to The Lorekeeper");
        lorekeeperButton.Pressed += () => ShowDialog(LorekeeperLine(progress));
        _npcList.AddChild(lorekeeperButton);

        if (progress.SecretEndingUnlocked)
        {
            var oracleButton = MakeNpcButton("Talk to The Oracle (???)");
            oracleButton.Pressed += () => ShowDialog(
                "The Oracle: You have gathered every echo. The hidden challenger awaits beyond the Archive's last door...");
            _npcList.AddChild(oracleButton);
        }
    }

    private static Button MakeNpcButton(string text)
    {
        return new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(250, 40),
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
        };
    }

    private static string CuratorLine(int formCount, int loreCount)
    {
        if (formCount == 0)
        {
            return "The Curator: Welcome, empty shell. Defeat a champion and archive your first form.";
        }

        var totalLore = LoreCatalog.AllFragmentIds.Count;
        if (totalLore > 0 && loreCount >= totalLore)
        {
            return "The Curator: Every echo accounted for. The Archive has never been so complete.";
        }

        return $"The Curator: {formCount} forms archived, {loreCount} echoes recovered. The collection grows.";
    }

    private static string BlacksmithLine(int formCount)
    {
        return formCount >= 3
            ? "The Blacksmith: Three champions' data? Your shell can temper builds few will ever match."
            : "The Blacksmith: Bring me more combat data, and I'll temper your forms.";
    }

    private static string LorekeeperLine(MvpProgressData progress)
    {
        if (progress.UnlockedLoreFragments.Count == 0)
        {
            return "The Lorekeeper: The Archive's history is still sealed. Defeat bosses to recover its echoes.";
        }

        var titles = new List<string>();
        foreach (var id in progress.UnlockedLoreFragments)
        {
            var fragment = LoreCatalog.GetFragment(id);
            if (fragment != null)
            {
                titles.Add($"\u2022 {fragment.Title}");
            }
        }

        return titles.Count == 0
            ? "The Lorekeeper: Faint echoes, not yet legible."
            : "The Lorekeeper — Recovered echoes:\n" + string.Join("\n", titles);
    }

    private void ShowDialog(string text)
    {
        _focusBeforeDialog = GetViewport().GuiGetFocusOwner();
        _dialogLabel.Text = text;
        _dialogPanel.Visible = true;
        _dialogCloseButton.CallDeferred("grab_focus");
    }

    private void CloseDialog()
    {
        _dialogPanel.Visible = false;
        if (_focusBeforeDialog is not null && GodotObject.IsInstanceValid(_focusBeforeDialog))
        {
            _focusBeforeDialog.CallDeferred("grab_focus");
        }
        else
        {
            _trainingButton.CallDeferred("grab_focus");
        }
    }

    private void ReturnToMainMenu()
    {
        SceneFlowCoordinator.TransitionTo(
            GetTree(),
            MainMenuScenePath,
            "RETURNING TO ARCHIVE MAP");
    }

    private void StartTrainingRoom()
    {
        if (ProjectMannequin.Data.MvpMissionSelection.TrySelectWorld("training_room"))
        {
            SceneFlowCoordinator.TransitionTo(
                GetTree(),
                "res://Scenes/Main.tscn",
                "LOADING TRAINING ROOM");
        }
    }
}
