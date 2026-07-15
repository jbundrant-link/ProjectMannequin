using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace ProjectMannequin.Data;

public static class RyuMugenAnimationCatalog
{
    private const string CatalogPath =
        "res://Assets/Sprites/Ryu/ryu_higgsfield_v5_animation_map.json";

    private static readonly Lazy<AnimationCatalog?> Catalog = new(LoadCatalog);

    public static void Apply(CharacterData character)
    {
        var catalog = Catalog.Value;
        if (catalog is null)
        {
            return;
        }

        foreach (var move in character.Moves)
        {
            if (!catalog.Moves.TryGetValue(move.Id, out var animation))
            {
                continue;
            }

            move.AnimationAtlasPath = catalog.Atlas.Path;
            move.AnimationAtlasColumns = catalog.Atlas.Columns;
            move.AnimationAtlasRows = catalog.Atlas.Rows;
            move.AnimationPixelSize = catalog.Atlas.PixelSize;
            move.AnimationGroundOffsetPixels = catalog.Atlas.GroundOffsetPixels;
            move.AnimationFrameSequence = new List<int>(animation.Frames);
            move.AnimationFrameDurations = new List<int>(animation.Durations);
        }
    }

    private static AnimationCatalog? LoadCatalog()
    {
        if (!Godot.FileAccess.FileExists(CatalogPath))
        {
            GD.PushWarning($"Ryu animation catalog is missing: {CatalogPath}");
            return null;
        }

        try
        {
            var json = Godot.FileAccess.GetFileAsString(CatalogPath);
            return JsonSerializer.Deserialize<AnimationCatalog>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                });
        }
        catch (Exception exception)
        {
            GD.PushWarning($"Ryu animation catalog could not be loaded: {exception.Message}");
            return null;
        }
    }

    private sealed class AnimationCatalog
    {
        [JsonPropertyName("atlas")]
        public AtlasData Atlas { get; set; } = new();

        [JsonPropertyName("moves")]
        public Dictionary<string, AnimationData> Moves { get; set; } = new();
    }

    private sealed class AtlasData
    {
        [JsonPropertyName("path")]
        public string Path { get; set; } = "";

        [JsonPropertyName("columns")]
        public int Columns { get; set; }

        [JsonPropertyName("rows")]
        public int Rows { get; set; }

        [JsonPropertyName("pixel_size")]
        public float PixelSize { get; set; }

        [JsonPropertyName("ground_offset_pixels")]
        public float GroundOffsetPixels { get; set; }
    }

    private sealed class AnimationData
    {
        [JsonPropertyName("frames")]
        public List<int> Frames { get; set; } = new();

        [JsonPropertyName("durations")]
        public List<int> Durations { get; set; } = new();
    }
}
