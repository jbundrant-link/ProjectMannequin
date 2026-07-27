using System.Linq;
using System.Text;
using Godot;
using ProjectMannequin.Progression;
using ProjectMannequin.Settings;

namespace ProjectMannequin.DebugTools;

/// <summary>
/// Deterministic tests for <see cref="SettingsStore"/> and
/// <see cref="SettingsData"/>.
/// </summary>
/// <remarks>
/// Exercises the store through JSON rather than the filesystem so the suite is
/// hermetic and never touches a developer's real settings file. Persistence is
/// disabled during headless runs, so a disk round-trip could not assert
/// anything here anyway. Runs via PROJECT_MANNEQUIN_SETTINGS_TEST=1.
/// </remarks>
public static class SettingsTests
{
    public static string Run()
    {
        var log = new StringBuilder();
        log.AppendLine("=== Settings Tests ===");
        var passed = 0;
        var failed = 0;

        void Check(bool condition, string label, string detail = "")
        {
            if (condition)
            {
                passed++;
                log.Append("  PASS ").Append(label).Append('\n');
            }
            else
            {
                failed++;
                log.Append("  FAIL ").Append(label);
                if (!string.IsNullOrWhiteSpace(detail))
                {
                    log.Append(" - ").Append(detail);
                }

                log.Append('\n');
            }
        }

        var defaults = new SettingsData().Normalize();
        Check(defaults.SchemaVersion == SettingsData.CurrentSchemaVersion
              && defaults.MasterVolume is > 0.0f and <= 1.0f
              && defaults.MusicVolume is > 0.0f and <= 1.0f
              && defaults.SfxVolume is > 0.0f and <= 1.0f
              && defaults.UiVolume is > 0.0f and <= 1.0f
              && defaults.ShakeIntensity is >= 0.0f and <= 1.0f
              && Mathf.IsEqualApprox(defaults.RenderScale, 1.0f)
              && Mathf.IsEqualApprox(defaults.HudScale, 1.0f)
              && defaults.VSyncEnabled
              && defaults.HoldToBlock
              && !defaults.ReducedFlash
              && !defaults.HighContrastTelegraphs
              && defaults.ActionBindings is not null,
            "defaults are audible, unscaled, and in range");

        // A hand-edited or older file must not be able to push the renderer,
        // audio, or HUD somewhere the options surface cannot represent.
        var wild = new SettingsData
        {
            MasterVolume = 7.5f,
            MusicVolume = -3.0f,
            SfxVolume = float.NaN,
            RenderScale = 12.0f,
            HudScale = 0.01f,
            HudSafeAreaInset = 5.0f,
            ShakeIntensity = -4.0f,
            ResolutionWidth = 4,
            ResolutionHeight = 1,
            DisplayMode = (WindowDisplayMode)99,
            ActionBindings = null!,
        }.Normalize();
        Check(Mathf.IsEqualApprox(wild.MasterVolume, 1.0f)
              && Mathf.IsZeroApprox(wild.MusicVolume)
              && Mathf.IsZeroApprox(wild.SfxVolume)
              && Mathf.IsEqualApprox(wild.RenderScale, SettingsData.MaximumRenderScale)
              && Mathf.IsEqualApprox(wild.HudScale, SettingsData.MinimumHudScale)
              && Mathf.IsEqualApprox(
                  wild.HudSafeAreaInset,
                  SettingsData.MaximumHudSafeAreaInset)
              && Mathf.IsZeroApprox(wild.ShakeIntensity)
              && wild.ResolutionWidth == SettingsData.MinimumResolutionWidth
              && wild.ResolutionHeight == SettingsData.MinimumResolutionHeight
              && wild.DisplayMode == WindowDisplayMode.Windowed
              && wild.ActionBindings is not null,
            "out-of-range values clamp instead of corrupting the surface");

        var authored = new SettingsData
        {
            MasterVolume = 0.42f,
            MusicVolume = 0.0f,
            SfxVolume = 0.77f,
            UiVolume = 0.5f,
            DisplayMode = WindowDisplayMode.Borderless,
            ResolutionWidth = 2560,
            ResolutionHeight = 1440,
            RenderScale = 1.25f,
            VSyncEnabled = false,
            HudScale = 1.2f,
            HudSafeAreaInset = 0.04f,
            ShakeIntensity = 0.0f,
            ReducedFlash = true,
            HighContrastTelegraphs = true,
            HoldToBlock = false,
        };
        authored.ActionBindings["block"] = "joypad_button_4";
        var round = SettingsStore.FromJson(SettingsStore.ToJson(authored));
        Check(Mathf.IsEqualApprox(round.MasterVolume, 0.42f)
              && Mathf.IsZeroApprox(round.MusicVolume)
              && Mathf.IsEqualApprox(round.SfxVolume, 0.77f)
              && round.DisplayMode == WindowDisplayMode.Borderless
              && round.ResolutionWidth == 2560
              && round.ResolutionHeight == 1440
              && Mathf.IsEqualApprox(round.RenderScale, 1.25f)
              && !round.VSyncEnabled
              && Mathf.IsEqualApprox(round.HudScale, 1.2f)
              && Mathf.IsEqualApprox(round.HudSafeAreaInset, 0.04f)
              && Mathf.IsZeroApprox(round.ShakeIntensity)
              && round.ReducedFlash
              && round.HighContrastTelegraphs
              && !round.HoldToBlock
              && round.ActionBindings.TryGetValue("block", out var binding)
              && binding == "joypad_button_4",
            "every authored setting survives a JSON round trip");

        // Silence and zero shake are legitimate player choices, so they must not
        // be treated as unset and replaced by defaults.
        Check(Mathf.IsZeroApprox(round.MusicVolume)
              && Mathf.IsZeroApprox(round.ShakeIntensity),
            "silence and zero shake persist rather than reverting to defaults");

        Check(SettingsStore.FromJson("{ not json ").SchemaVersion
                  == SettingsData.CurrentSchemaVersion
              && SettingsStore.FromJson("").ActionBindings is not null
              && SettingsStore.FromJson("null").ActionBindings is not null,
            "malformed settings degrade to usable defaults");

        var partial = SettingsStore.FromJson("{\"MasterVolume\":0.25}");
        Check(Mathf.IsEqualApprox(partial.MasterVolume, 0.25f)
              && Mathf.IsEqualApprox(partial.HudScale, 1.0f)
              && partial.VSyncEnabled,
            "partial settings keep defaults for absent fields");

        var mutated = authored.Clone();
        mutated.MasterVolume = 0.1f;
        Check(!Mathf.IsEqualApprox(mutated.MasterVolume, authored.MasterVolume)
              && mutated.ActionBindings.ContainsKey("block")
              && !ReferenceEquals(mutated.ActionBindings, authored.ActionBindings),
            "clone is independent of its source");

        Check(SettingsStore.SettingsSavePath
                  != "user://project_mannequin_mvp_progress.json"
              && SettingsStore.SettingsSavePath
                  != "user://project_mannequin_input_settings.json"
              && SettingsStore.SettingsSavePath.StartsWith("user://"),
            "settings persist separately from progression and run saves");

        Check(SettingsStore.IsPersistenceDisabled,
            "headless suites never write a developer's settings file");

        var model = new OptionsModel(new SettingsData());
        var rows = model.BuildRows();
        Check(rows.Count == model.RowCount
              && rows.Count > 0
              && rows.All(row => !string.IsNullOrWhiteSpace(row.Label))
              && rows.Any(row => row.Id == "shake_intensity")
              && rows.Any(row => row.Id == "reduced_flash")
              && rows.Any(row => row.Id == "high_contrast")
              && rows.Any(row => row.Id == "hold_to_block")
              && rows.Any(row => row.Id == "reset_defaults")
              && rows.All(row => row.Kind != OptionRowKind.Action
                  || !string.IsNullOrWhiteSpace(row.Description)),
            "every option row is labelled and the accessibility rows exist");

        // A pad flick past either end must wrap rather than dead-end.
        model.MoveSelection(-1);
        var wrappedToLast = model.SelectedIndex == model.RowCount - 1;
        model.MoveSelection(1);
        var wrappedToFirst = model.SelectedIndex == 0;
        Check(wrappedToLast && wrappedToFirst,
            "selection wraps at both ends");

        var volumeModel = new OptionsModel(new SettingsData { MasterVolume = 0.5f });
        volumeModel.Adjust(1);
        var raised = volumeModel.Settings.MasterVolume;
        volumeModel.Adjust(-1);
        var restored = volumeModel.Settings.MasterVolume;
        for (var step = 0; step < 100; step++)
        {
            volumeModel.Adjust(1);
        }

        Check(raised > 0.5f
              && Mathf.IsEqualApprox(restored, 0.5f)
              && Mathf.IsEqualApprox(volumeModel.Settings.MasterVolume, 1.0f)
              && volumeModel.IsDirty,
            "adjusting a slider steps, reverses, and saturates at its bound");

        var shakeModel = new OptionsModel(new SettingsData());
        while (shakeModel.SelectedId != "shake_intensity")
        {
            shakeModel.MoveSelection(1);
        }

        for (var step = 0; step < 40; step++)
        {
            shakeModel.Adjust(-1);
        }

        Check(Mathf.IsZeroApprox(shakeModel.Settings.ShakeIntensity)
              && shakeModel.BuildRow("shake_intensity").ValueText == "0%",
            "screen shake reaches exactly zero and reports it");

        var toggleModel = new OptionsModel(new SettingsData());
        while (toggleModel.SelectedId != "reduced_flash")
        {
            toggleModel.MoveSelection(1);
        }

        var beforeToggle = toggleModel.Settings.ReducedFlash;
        toggleModel.Activate();
        var afterActivate = toggleModel.Settings.ReducedFlash;
        toggleModel.Adjust(1);
        Check(afterActivate != beforeToggle
              && toggleModel.Settings.ReducedFlash == beforeToggle,
            "toggle rows flip on both activate and adjust");

        var resetModel = new OptionsModel(new SettingsData
        {
            MasterVolume = 0.1f,
            ShakeIntensity = 0.0f,
            ReducedFlash = true,
            HoldToBlock = false,
        });
        while (resetModel.SelectedId != "reset_defaults")
        {
            resetModel.MoveSelection(1);
        }

        var closes = resetModel.Activate();
        var defaultsAgain = new SettingsData().Normalize();
        Check(!closes
              && Mathf.IsEqualApprox(
                  resetModel.Settings.MasterVolume,
                  defaultsAgain.MasterVolume)
              && Mathf.IsEqualApprox(
                  resetModel.Settings.ShakeIntensity,
                  defaultsAgain.ShakeIntensity)
              && resetModel.Settings.ReducedFlash == defaultsAgain.ReducedFlash
              && resetModel.Settings.HoldToBlock == defaultsAgain.HoldToBlock
              && resetModel.IsDirty,
            "reset restores defaults without closing the surface");

        var resolutionModel = new OptionsModel(new SettingsData
        {
            ResolutionWidth = 1234,
            ResolutionHeight = 777,
        });
        while (resolutionModel.SelectedId != "resolution")
        {
            resolutionModel.MoveSelection(1);
        }

        resolutionModel.Adjust(1);
        var landedWidth = resolutionModel.Settings.ResolutionWidth;
        for (var step = 0; step < 20; step++)
        {
            resolutionModel.Adjust(-1);
        }

        Check(landedWidth is 1280 or 1600 or 1920 or 2560 or 3840
              && resolutionModel.Settings.ResolutionWidth == 1280
              && resolutionModel.Settings.ResolutionHeight == 720,
            "an unlisted resolution steps onto the supported ladder");

        // --- Accessibility contract -------------------------------------

        var strobePeriods = new[]
        {
            AccessibilityRuntime.StrobePeriods.FormSwapMilliseconds,
            AccessibilityRuntime.StrobePeriods.ParryMilliseconds,
            AccessibilityRuntime.StrobePeriods.GuardBreakMilliseconds,
            AccessibilityRuntime.StrobePeriods.SuperStartupMilliseconds,
            AccessibilityRuntime.StrobePeriods.HitstunMilliseconds,
        };

        // Reduced flash must remove the flicker outright, not merely slow it,
        // so sample a whole cycle of every declared strobe and require a
        // constant result.
        var everyStrobeIsSteady = true;
        foreach (var halfPeriod in strobePeriods)
        {
            var expectedScalar = AccessibilityRuntime.StrobeScalar(
                0UL, halfPeriod, 0.6f, 1.0f, reducedFlash: true);
            for (var millisecond = 0UL; millisecond <= (ulong)(halfPeriod * 4); millisecond++)
            {
                if (!AccessibilityRuntime.StrobeOn(millisecond, halfPeriod, reducedFlash: true))
                {
                    everyStrobeIsSteady = false;
                }

                var scalar = AccessibilityRuntime.StrobeScalar(
                    millisecond, halfPeriod, 0.6f, 1.0f, reducedFlash: true);
                if (!Mathf.IsEqualApprox(scalar, expectedScalar))
                {
                    everyStrobeIsSteady = false;
                }
            }
        }

        Check(everyStrobeIsSteady && Mathf.IsEqualApprox(
                  AccessibilityRuntime.StrobeScalar(
                      0UL, 80, 0.6f, 1.0f, reducedFlash: true),
                  0.8f),
            "reduced flash holds every declared strobe perfectly steady");

        // The same strobes must still actually blink when the setting is off,
        // otherwise the test above would pass on a dead code path.
        var strobesStillAnimate = strobePeriods.All(halfPeriod =>
            AccessibilityRuntime.StrobePhase(0UL, halfPeriod)
            != AccessibilityRuntime.StrobePhase((ulong)halfPeriod, halfPeriod));
        Check(strobesStillAnimate,
            "strobes still animate when reduced flash is off");

        Check(Mathf.IsEqualApprox(AccessibilityRuntime.FlashesPerSecond(80), 6.25f)
              && Mathf.IsEqualApprox(AccessibilityRuntime.FlashesPerSecond(500), 1.0f)
              && Mathf.IsZeroApprox(AccessibilityRuntime.FlashesPerSecond(0)),
            "flash rate maths matches the declared half periods");

        Check(Mathf.IsEqualApprox(
                  AccessibilityRuntime.Pulse(0UL, 0.014f, reducedFlash: true), 0.5f)
              && Mathf.IsEqualApprox(
                  AccessibilityRuntime.Pulse(12345UL, 0.014f, reducedFlash: true), 0.5f),
            "reduced flash flattens the telegraph pulse");

        // The core of the non-colour contract: across every pulse and progress
        // sample, a warning telegraph can never reach the opacity of a live
        // one, so the phase survives total colour blindness.
        var bandsStayApart = true;
        var countdownAlwaysContracts = true;
        foreach (var highContrast in new[] { false, true })
        {
            var maximumWarning = float.MinValue;
            var minimumActive = float.MaxValue;
            for (var step = 0; step <= 20; step++)
            {
                var sample = step / 20.0f;
                var warning = AccessibilityRuntime.ResolveTelegraph(
                    isWarning: true, progress: sample, pulse: sample, highContrast);
                var active = AccessibilityRuntime.ResolveTelegraph(
                    isWarning: false, progress: sample, pulse: sample, highContrast);

                maximumWarning = Mathf.Max(maximumWarning, warning.Alpha);
                minimumActive = Mathf.Min(minimumActive, active.Alpha);

                if (!warning.CountdownVisible
                    || active.CountdownVisible
                    || !Mathf.IsEqualApprox(warning.CountdownScale, 1.0f - sample))
                {
                    countdownAlwaysContracts = false;
                }
            }

            // The declared bounds are documentation of the band, so compare
            // with a tolerance: 0.30f + 0.18f lands a float ulp above 0.48f.
            const float bandTolerance = 0.0001f;
            if (maximumWarning >= minimumActive
                || maximumWarning
                    > AccessibilityRuntime.MaximumWarningAlpha(highContrast) + bandTolerance
                || minimumActive
                    < AccessibilityRuntime.MinimumActiveAlpha(highContrast) - bandTolerance)
            {
                bandsStayApart = false;
            }
        }

        Check(bandsStayApart,
            "warning and active telegraph opacity bands never overlap");
        Check(countdownAlwaysContracts,
            "the countdown marker exists only while winding up and contracts to zero");

        Check(AccessibilityRuntime.ResolveTelegraph(true, 0.5f, 1.0f, highContrast: true).Alpha
                  > AccessibilityRuntime.ResolveTelegraph(true, 0.5f, 1.0f, highContrast: false).Alpha
              && AccessibilityRuntime.ResolveTelegraph(false, 0.0f, 0.0f, highContrast: true).EmissionEnergy
                  > AccessibilityRuntime.ResolveTelegraph(false, 0.0f, 0.0f, highContrast: false).EmissionEnergy,
            "high contrast telegraphs raise both opacity and emission");

        Check(Mathf.IsEqualApprox(
                  AccessibilityRuntime.Oscillate(0UL, 0.03f, 0.82f, 0.18f, reducedFlash: true),
                  0.82f)
              && Mathf.IsEqualApprox(
                  AccessibilityRuntime.Oscillate(9999UL, 0.03f, 0.82f, 0.18f, reducedFlash: true),
                  0.82f)
              && !Mathf.IsEqualApprox(
                  AccessibilityRuntime.Oscillate(52UL, 0.03f, 0.82f, 0.18f, reducedFlash: false),
                  0.82f),
            "reduced flash holds smooth oscillations at their centre value");

        // The boss aura covers a large part of the frame, so it must stay under
        // the WCAG 2.3.1 three-flash ceiling. The character state strobes are
        // deliberately above it and are covered by ReducedFlash instead.
        Check(AccessibilityRuntime.HertzForAngularRate(
                  AccessibilityRuntime.BossAuraFlickerRate)
              < AccessibilityRuntime.MaximumSafeFlashesPerSecond
              && AccessibilityRuntime.HertzForAngularRate(0.03f) > 4.0f,
            "the large-area boss aura flickers below the WCAG three-flash ceiling");

        var restingColor = Colors.White;
        var flashColor = new Color(1.0f, 0.34f, 0.24f);
        var softened = AccessibilityRuntime.SoftenImpactFlash(
            flashColor, restingColor, reducedFlash: true);
        Check(AccessibilityRuntime.SoftenImpactFlash(
                  flashColor, restingColor, reducedFlash: false) == flashColor
              && softened != flashColor
              && softened.G > flashColor.G
              && softened.B > flashColor.B,
            "reduced flash softens impact flashes toward the resting colour");

        // --- Save schema robustness -------------------------------------

        Check(SaveSchema.Evaluate(2, 2, 1) == SaveCompatibility.Current
              && SaveSchema.Evaluate(1, 2, 1) == SaveCompatibility.Migrated
              && SaveSchema.Evaluate(3, 2, 1) == SaveCompatibility.FutureVersion
              && SaveSchema.Evaluate(0, 2, 1) == SaveCompatibility.Unsupported
              && SaveSchema.Evaluate(-5, 2, 1) == SaveCompatibility.Unsupported
              && SaveSchema.Evaluate(1, 5, 3) == SaveCompatibility.Unsupported,
            "schema evaluation classifies current, older, newer, and nonsense versions");

        // The important one. Deserializing a newer file drops fields this build
        // does not know, so writing it back would delete the player's data.
        Check(!SaveSchema.MayOverwrite(SaveCompatibility.FutureVersion)
              && !SaveSchema.MayOverwrite(SaveCompatibility.Unsupported)
              && SaveSchema.MayOverwrite(SaveCompatibility.Current)
              && SaveSchema.MayOverwrite(SaveCompatibility.Migrated),
            "a newer or unsupported save is never overwritten by this build");

        Check(!SaveSchema.ShouldShowSaveIndicator(null, 1000UL, 500UL)
              && SaveSchema.ShouldShowSaveIndicator(1000UL, 1000UL, 500UL)
              && SaveSchema.ShouldShowSaveIndicator(1000UL, 1499UL, 500UL)
              && !SaveSchema.ShouldShowSaveIndicator(1000UL, 1500UL, 500UL)
              // A backwards clock must not wrap the unsigned subtraction and
              // pin the indicator on forever.
              && !SaveSchema.ShouldShowSaveIndicator(1000UL, 999UL, 500UL),
            "the save indicator shows for its hold window and survives a backwards clock");

        var eraseModel = new OptionsModel(new SettingsData());
        while (eraseModel.SelectedId != "erase_save_data")
        {
            eraseModel.MoveSelection(1);
        }

        // One press must only arm, never erase.
        var firstPressClosed = eraseModel.Activate();
        Check(!firstPressClosed
              && eraseModel.EraseArmed
              && !eraseModel.EraseRequested
              && eraseModel.BuildRow("erase_save_data").Label.Contains("CONFIRM"),
            "erasing save data arms on the first press instead of erasing");

        // Navigating away must disarm, so a later stray Accept cannot wipe.
        eraseModel.MoveSelection(1);
        Check(!eraseModel.EraseArmed && !eraseModel.EraseRequested,
            "moving off the erase row disarms the confirmation");

        while (eraseModel.SelectedId != "erase_save_data")
        {
            eraseModel.MoveSelection(1);
        }

        eraseModel.Activate();
        var confirmedClosed = eraseModel.Activate();
        Check(confirmedClosed
              && eraseModel.EraseRequested
              && !eraseModel.EraseArmed,
            "a second confirming press requests the erase and closes the surface");

        eraseModel.ClearEraseRequest();
        Check(!eraseModel.EraseRequested,
            "the erase request is consumable so it cannot fire twice");

        // Erasing must remove backups too, or the next load silently restores
        // the data the player asked to delete.
        Check(SaveDataReset.SavePaths.Length == 4
              && SaveDataReset.SavePaths.All(path => path.StartsWith("user://", System.StringComparison.Ordinal))
              && SaveDataReset.SavePaths.Distinct().Count() == SaveDataReset.SavePaths.Length
              && SaveDataReset.EraseAll() == 0,
            "reset covers every save path and refuses to run while persistence is disabled");

        log.Append($"=== {passed} passed, {failed} failed ===");
        return log.ToString();
    }
}
