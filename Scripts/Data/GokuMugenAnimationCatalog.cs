using System.Collections.Generic;

namespace ProjectMannequin.Data;

public static class GokuMugenAnimationCatalog
{
    public const string BaseSpecialAtlas =
        "res://Assets/Sprites/Goku/goku_astral_base_specials_higgsfield_v1_sheet.png";
    public const string KaiokenSpecialAtlas =
        "res://Assets/Sprites/Goku/goku_astral_kaioken_specials_higgsfield_v1_sheet.png";
    public const string FalseSuperSpecialAtlas =
        "res://Assets/Sprites/Goku/goku_astral_false_super_specials_higgsfield_v1_sheet.png";
    public const string SuperSaiyan1SpecialAtlas =
        "res://Assets/Sprites/Goku/goku_astral_ss1_specials_higgsfield_v1_sheet.png";
    public const string SuperSaiyan2SpecialAtlas =
        "res://Assets/Sprites/Goku/goku_astral_ss2_specials_higgsfield_v1_sheet.png";
    public const string SuperSaiyan3SpecialAtlas =
        "res://Assets/Sprites/Goku/goku_astral_ss3_specials_higgsfield_v1_sheet.png";
    public const string SuperSaiyan4SpecialAtlas =
        "res://Assets/Sprites/Goku/goku_astral_ss4_specials_higgsfield_v1_sheet.png";
    public const string GodSpecialAtlas =
        "res://Assets/Sprites/Goku/goku_astral_god_specials_higgsfield_v1_sheet.png";
    public const string BlueSpecialAtlas =
        "res://Assets/Sprites/Goku/goku_astral_blue_specials_higgsfield_v1_sheet.png";
    public const string BlueKaiokenSpecialAtlas =
        "res://Assets/Sprites/Goku/goku_astral_blue_kaioken_specials_higgsfield_v1_sheet.png";
    public const string UltraInstinctSignSpecialAtlas =
        "res://Assets/Sprites/Goku/goku_astral_ui_sign_specials_higgsfield_v1_sheet.png";
    public const string InstinctSpecialAtlas =
        "res://Assets/Sprites/Goku/goku_astral_instinct_specials_higgsfield_v1_sheet.png";

    private static readonly Dictionary<string, string> SpecialAtlases = new()
    {
        ["base"] = BaseSpecialAtlas,
        ["kaioken"] = KaiokenSpecialAtlas,
        ["false_super"] = FalseSuperSpecialAtlas,
        ["ss1"] = SuperSaiyan1SpecialAtlas,
        ["ss2"] = SuperSaiyan2SpecialAtlas,
        ["ss3"] = SuperSaiyan3SpecialAtlas,
        ["ss4"] = SuperSaiyan4SpecialAtlas,
        ["god"] = GodSpecialAtlas,
        ["blue"] = BlueSpecialAtlas,
        ["blue_kaioken"] = BlueKaiokenSpecialAtlas,
        ["ui_sign"] = UltraInstinctSignSpecialAtlas,
        ["instinct"] = InstinctSpecialAtlas,
    };

    private static readonly Dictionary<string, AnimationDefinition> Definitions = new()
    {
        ["goku_lp"] = Definition(Range(30, 4)),
        ["goku_mp"] = Definition(Range(34, 4)),
        ["goku_hp"] = Definition(Range(38, 6)),
        ["goku_lk"] = Definition(Range(44, 6)),
        ["goku_mk"] = Definition(Range(50, 8)),
        ["goku_hk"] = Definition(Range(58, 6)),
        ["goku_2lp"] = Definition(Range(72, 3)),
        ["goku_2mp"] = Definition(Range(75, 4)),
        ["goku_2hp"] = Definition(Range(79, 6)),
        ["goku_2lk"] = Definition(Range(85, 3)),
        ["goku_2mk"] = Definition(Range(85, 6)),
        ["goku_2hk"] = Definition(Range(85, 6)),
        ["goku_air_lp"] = Definition(Range(91, 10)),
        ["goku_air_mp"] = Definition(Range(101, 10)),
        ["goku_air_hp"] = Definition(Range(111, 9)),
        ["goku_air_lk"] = Definition(Range(120, 10)),
        ["goku_air_mk"] = Definition(Range(130, 8)),
        ["goku_air_hk"] = Definition(Range(130, 8)),

        // AIR 2300: grounded Kamehameha.
        ["goku_kamehameha_light"] = Special(
            Range(22, 9),
            3, 3, 3, 3, 20, 3, 60, 3, 3),
        ["goku_kamehameha_heavy"] = Special(
            Range(22, 9),
            3, 3, 3, 3, 20, 3, 60, 3, 3),
        ["goku_super_kamehameha"] = Special(
            Range(22, 9),
            3, 3, 3, 3, 20, 3, 60, 3, 3),
        ["goku_god_kamehameha"] = Special(
            Range(22, 9),
            3, 3, 3, 3, 20, 3, 60, 3, 3),

        // AIR 1400: rising anti-air.
        ["goku_dragon_rising"] = Special(
            Range(11, 6),
            3, 3, 3, 5, 3, 3),

        // AIR 700 followed by AIR 1000: rush startup and diving finish.
        ["goku_dragon_flash"] = Special(
            new[] { 0, 1, 2, 3, 4 },
            6, 5, 3, 3, 4),

        // AIR 3600 includes one authored invisible teleport cell.
        ["goku_instant_step"] = Special(
            Range(43, 7),
            4, 4, 6, 6, 4, 4, 4),
        ["goku_flight_cancel"] = Special(
            Range(43, 7),
            4, 4, 6, 6, 4, 4, 4),

        // AIR 1800: aerial Meteor Smash.
        ["goku_meteor_smash"] = Special(
            Range(17, 5),
            10, 3, 3, 1, 1),
        ["goku_flight_dive"] = Special(
            Range(17, 5),
            10, 3, 3, 1, 1),

        // AIR 2350: airborne Kamehameha/ki pressure.
        ["goku_flight_ki_blast"] = Special(
            Range(31, 9),
            3, 3, 3, 3, 20, 3, 60, 3, 3),
        ["goku_flight_rush"] = Special(
            Range(0, 3),
            6, 5, 3),

        // AIR 6500: Spirit Bomb charge and release.
        ["goku_spirit_bomb"] = Special(
            Range(50, 5),
            5, 5, 50, 5, 5),

        ["goku_instinct_rush"] = Special(
            new[] { 0, 1, 2, 11, 12, 13, 14, 15, 16, 43, 44, 45 },
            3, 3, 2, 2, 2, 2, 2, 2, 3, 2, 2, 3),
    };

    public static void Apply(IEnumerable<MoveData> moves)
    {
        foreach (var move in moves)
        {
            if (!Definitions.TryGetValue(move.Id, out var definition))
            {
                continue;
            }

            move.AnimationFrameSequence = new List<int>(definition.Frames);
            move.AnimationFrameDurations = new List<int>(definition.Durations);
            if (!definition.UsesSpecialAtlas)
            {
                continue;
            }

            move.AnimationAtlasPath = BaseSpecialAtlas;
            move.AnimationVariantAtlasPaths = new Dictionary<string, string>(SpecialAtlases);
            move.AnimationAtlasColumns = 8;
            move.AnimationAtlasRows = 8;
            move.AnimationPixelSize = 0.0144f;
            move.AnimationGroundOffsetPixels = 152.0f;
        }
    }

    public static SpriteAnimationClipData CreateBlueTransitionClip()
    {
        return new SpriteAnimationClipData
        {
            AtlasPath =
                "res://Assets/Sprites/Goku/goku_astral_transform_blue_higgsfield_v1_sheet.png",
            AtlasColumns = 8,
            AtlasRows = 3,
            PixelSize = 0.0144f,
            GroundOffsetPixels = 152.0f,
            Frames = Range(0, 17),
            Durations = Repeat(3, 17),
        };
    }

    public static SpriteAnimationClipData CreateInstinctTransitionClip()
    {
        return new SpriteAnimationClipData
        {
            AtlasPath =
                "res://Assets/Sprites/Goku/goku_astral_transform_instinct_higgsfield_v1_sheet.png",
            AtlasColumns = 8,
            AtlasRows = 8,
            PixelSize = 0.0144f,
            GroundOffsetPixels = 152.0f,
            Frames = Range(0, 64),
            Durations = Repeat(2, 64),
        };
    }

    public static SpriteAnimationClipData CreateInstinctEvadeClip(
        string atlasPath = InstinctSpecialAtlas)
    {
        return new SpriteAnimationClipData
        {
            AtlasPath = atlasPath,
            AtlasColumns = 8,
            AtlasRows = 8,
            PixelSize = 0.0144f,
            GroundOffsetPixels = 152.0f,
            Frames = Range(43, 7),
            Durations = new List<int> { 2, 2, 3, 3, 2, 2, 2 },
        };
    }

    public static SpriteAnimationClipData CreateFormTransitionClip(string atlasPath)
    {
        return new SpriteAnimationClipData
        {
            AtlasPath = atlasPath,
            AtlasColumns = 8,
            AtlasRows = 18,
            PixelSize = 0.0144f,
            GroundOffsetPixels = 152.0f,
            Frames = Range(0, 6),
            Durations = new List<int> { 3, 3, 3, 3, 4, 6 },
        };
    }

    private static AnimationDefinition Definition(IReadOnlyList<int> frames)
    {
        return new AnimationDefinition(frames, Repeat(1, frames.Count), false);
    }

    private static AnimationDefinition Special(
        IReadOnlyList<int> frames,
        params int[] durations)
    {
        return new AnimationDefinition(frames, durations, true);
    }

    private static List<int> Range(int start, int count)
    {
        var frames = new List<int>(count);
        for (var index = 0; index < count; index++)
        {
            frames.Add(start + index);
        }
        return frames;
    }

    private static List<int> Repeat(int value, int count)
    {
        var values = new List<int>(count);
        for (var index = 0; index < count; index++)
        {
            values.Add(value);
        }
        return values;
    }

    private sealed record AnimationDefinition(
        IReadOnlyList<int> Frames,
        IReadOnlyList<int> Durations,
        bool UsesSpecialAtlas);
}
