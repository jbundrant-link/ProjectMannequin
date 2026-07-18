using System.Collections.Generic;
using Godot;
using ProjectMannequin.Core;
using ProjectMannequin.Data;
using ProjectMannequin.Presentation;

namespace ProjectMannequin.UI;

/// <summary>
/// Resolves a portrait texture for a form shown in the Form Select overlay.
///
/// Priority:
/// 1. An explicit <see cref="CharacterData.SelectPortraitPath"/> resource.
/// 2. A derived asset at <c>res://Assets/UI/FormSelect/&lt;id&gt;.png</c>.
/// 3. A runtime crop of the form's idle frame — cropped from its sprite sheet
///    when it has one, or from a procedurally generated mannequin sheet when it
///    does not (the base mannequin and Archive Knight render procedurally).
///
/// Results are cached per form id. The runtime crop keeps the feature working
/// for every form with no committed art; explicit/derived paths let hand-drawn
/// portraits drop in later without code changes.
/// </summary>
public static class FormSelectPortraitResolver
{
    private const string DerivedDirectory = "res://Assets/UI/FormSelect/";
    private const float AlphaThreshold = 0.12f;

    private static readonly Dictionary<string, Texture2D?> TextureCache = new();
    private static readonly Dictionary<string, (Texture2D Source, Rect2I Region)?> BaseCache = new();

    public static Texture2D? Resolve(CharacterData? form, bool bust = false)
    {
        if (form is null || string.IsNullOrWhiteSpace(form.Id))
        {
            return null;
        }

        var key = form.Id + (bust ? "|bust" : "|full");
        if (TextureCache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var texture = Build(form, bust);
        TextureCache[key] = texture;
        return texture;
    }

    private static Texture2D? Build(CharacterData form, bool bust)
    {
        if (!string.IsNullOrWhiteSpace(form.SelectPortraitPath)
            && ResourceLoader.Exists(form.SelectPortraitPath))
        {
            return FrameExplicitPortrait(
                GD.Load<Texture2D>(form.SelectPortraitPath),
                bust);
        }

        var derivedPath = DerivedDirectory + form.Id + ".png";
        if (ResourceLoader.Exists(derivedPath))
        {
            return FrameExplicitPortrait(GD.Load<Texture2D>(derivedPath), bust);
        }

        var crop = GetBase(form);
        if (crop is null)
        {
            return null;
        }

        var (source, full) = crop.Value;
        var region = bust ? ToBust(full) : full;
        return new AtlasTexture
        {
            Atlas = source,
            Region = new Rect2(region.Position.X, region.Position.Y, region.Size.X, region.Size.Y),
            FilterClip = true,
        };
    }

    private static Texture2D? FrameExplicitPortrait(Texture2D? source, bool bust)
    {
        if (source is null || !bust)
        {
            return source;
        }

        var fullCell = new Rect2I(0, 0, source.GetWidth(), source.GetHeight());
        var full = ComputeContentRegion(source, fullCell) ?? fullCell;
        var region = ToBust(full);
        return new AtlasTexture
        {
            Atlas = source,
            Region = new Rect2(region.Position.X, region.Position.Y, region.Size.X, region.Size.Y),
            FilterClip = true,
        };
    }

    private static (Texture2D Source, Rect2I Region)? GetBase(CharacterData form)
    {
        if (BaseCache.TryGetValue(form.Id, out var cached))
        {
            return cached;
        }

        var result = ComputeBase(form);
        BaseCache[form.Id] = result;
        return result;
    }

    private static (Texture2D Source, Rect2I Region)? ComputeBase(CharacterData form)
    {
        Texture2D? source;
        int columns;
        int rows;

        if (!string.IsNullOrWhiteSpace(form.SpriteSheetPath)
            && ResourceLoader.Exists(form.SpriteSheetPath))
        {
            source = GD.Load<Texture2D>(form.SpriteSheetPath);
            columns = Mathf.Max(1, form.SpriteSheetColumns);
            rows = Mathf.Max(1, form.SpriteSheetRows);
        }
        else
        {
            source = ProceduralMannequinSpriteSheetFactory.Create(BuildPalette(form));
            columns = ProceduralMannequinSpriteSheetFactory.Columns;
            rows = ProceduralMannequinSpriteSheetFactory.Rows;
        }

        if (source is null)
        {
            return null;
        }

        var cellWidth = source.GetWidth() / columns;
        var cellHeight = source.GetHeight() / rows;
        if (cellWidth <= 0 || cellHeight <= 0)
        {
            return null;
        }

        // Idle pose lives in the first cell (row 0, column 0) for every profile.
        var idleCell = new Rect2I(0, 0, cellWidth, cellHeight);
        var region = ComputeContentRegion(source, idleCell) ?? idleCell;
        return (source, region);
    }

    // Crops the upper half into a narrow portrait window so wide weapons or
    // pauldrons cannot shrink the face and torso inside compact square wells.
    private static Rect2I ToBust(Rect2I full)
    {
        var height = Mathf.Clamp(
            Mathf.RoundToInt(full.Size.Y * 0.50f),
            1,
            full.Size.Y);
        var width = Mathf.Clamp(
            Mathf.RoundToInt(height * 0.82f),
            1,
            full.Size.X);
        var x = full.Position.X + (full.Size.X - width) / 2;
        return new Rect2I(x, full.Position.Y, width, height);
    }

    /// <summary>
    /// Returns the tight alpha bounding box (with light padding) of the idle
    /// cell, or null when the pixels cannot be read so the caller can fall back
    /// to the full cell.
    /// </summary>
    private static Rect2I? ComputeContentRegion(Texture2D source, Rect2I cell)
    {
        var image = source.GetImage();
        if (image is null)
        {
            return null;
        }

        if (image.IsCompressed() && image.Decompress() != Error.Ok)
        {
            return null;
        }

        var startX = cell.Position.X;
        var startY = cell.Position.Y;
        var endX = Mathf.Min(image.GetWidth(), cell.Position.X + cell.Size.X);
        var endY = Mathf.Min(image.GetHeight(), cell.Position.Y + cell.Size.Y);

        var minX = endX;
        var minY = endY;
        var maxX = startX;
        var maxY = startY;
        var found = false;

        for (var y = startY; y < endY; y++)
        {
            for (var x = startX; x < endX; x++)
            {
                if (image.GetPixel(x, y).A <= AlphaThreshold)
                {
                    continue;
                }

                found = true;
                if (x < minX) minX = x;
                if (y < minY) minY = y;
                if (x > maxX) maxX = x;
                if (y > maxY) maxY = y;
            }
        }

        if (!found)
        {
            return null;
        }

        var padX = Mathf.RoundToInt(cell.Size.X * 0.06f);
        var padY = Mathf.RoundToInt(cell.Size.Y * 0.06f);
        minX = Mathf.Max(startX, minX - padX);
        minY = Mathf.Max(startY, minY - padY);
        maxX = Mathf.Min(endX - 1, maxX + padX);
        maxY = Mathf.Min(endY - 1, maxY + padY);

        return new Rect2I(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    private static MannequinSpritePalette BuildPalette(CharacterData form)
    {
        // Mirrors CharacterVisualComponent's player palette so procedural
        // portraits match how the form renders in the fight.
        var accent = GameConstants.StandardPlayerColors[0];

        if (form.Id.Contains("knight"))
        {
            return new MannequinSpritePalette(
                new Color(0.04f, 0.04f, 0.07f),
                new Color(0.68f, 0.74f, 0.82f),
                new Color(0.92f, 0.96f, 1.0f),
                new Color(0.28f, 0.34f, 0.45f),
                new Color(0.08f, 0.07f, 0.11f),
                accent);
        }

        return new MannequinSpritePalette(
            new Color(0.055f, 0.035f, 0.055f),
            new Color(0.86f, 0.61f, 0.43f),
            new Color(1.0f, 0.82f, 0.60f),
            new Color(0.48f, 0.27f, 0.25f),
            new Color(0.11f, 0.07f, 0.10f),
            accent);
    }
}
