using Godot;

namespace ProjectMannequin.Presentation;

/// <summary>
/// Frames the fight by darkening only the top and bottom edges.
/// </summary>
/// <remarks>
/// Godot 4's <c>Environment</c> has no vignette, so this is a screen-space
/// overlay rather than a post-process setting.
///
/// It is deliberately NOT the conventional radial vignette. This is a
/// belt-scroller: the left and right edges are exactly where a cornered player
/// is fighting for their life, and a radial falloff dims the character who can
/// least afford it at the moment they can least afford it. Framing is worth
/// something; readability during a corner pressure situation is worth more.
/// So the falloff runs vertically only and the full playable width stays at
/// full brightness.
///
/// The bands are placed from a measured frame rather than by eye. Fighters
/// occupy roughly 0.45-0.80 of frame height, so the top band stops well above
/// them and the bottom band starts below their feet, over what is only bright
/// floor. The result darkens dead space and leaves the action untouched.
/// </remarks>
public partial class StageVignette : CanvasLayer
{
    /// <summary>Node name, matched by the clean-capture hide list.</summary>
    public const string NodeName = "StageVignette";

    /// <summary>Fraction of frame height over which the top band fades out.</summary>
    public const float TopBandEnd = 0.24f;

    /// <summary>Fraction of frame height at which the bottom band starts.</summary>
    public const float BottomBandStart = 0.88f;

    /// <summary>Peak darkening at the very top edge.</summary>
    public const float TopStrength = 0.38f;

    /// <summary>Peak darkening at the very bottom edge.</summary>
    public const float BottomStrength = 0.30f;

    /// <summary>Lowest frame fraction a fighter is expected to occupy.</summary>
    public const float FighterBandTop = 0.45f;

    /// <summary>Highest frame fraction a fighter is expected to occupy.</summary>
    public const float FighterBandBottom = 0.80f;

    private const int GradientHeight = 256;
    private const int GradientWidth = 4;

    /// <summary>
    /// Darkening applied at a given fraction of frame height.
    /// </summary>
    /// <remarks>
    /// Pure so the profile can be asserted without a scene tree. Uses a
    /// smoothstep rather than a linear ramp because a linear alpha ramp across
    /// a large flat area shows visible banding on an 8-bit display.
    /// </remarks>
    public static float AlphaAt(float heightFraction)
    {
        var f = Mathf.Clamp(heightFraction, 0.0f, 1.0f);

        if (f < TopBandEnd)
        {
            var t = 1.0f - (f / TopBandEnd);
            return TopStrength * Smoothstep(t);
        }

        if (f > BottomBandStart)
        {
            var t = (f - BottomBandStart) / (1.0f - BottomBandStart);
            return BottomStrength * Smoothstep(t);
        }

        return 0.0f;
    }

    private static float Smoothstep(float t)
    {
        var clamped = Mathf.Clamp(t, 0.0f, 1.0f);
        return clamped * clamped * (3.0f - 2.0f * clamped);
    }

    public static StageVignette Create()
    {
        var vignette = new StageVignette
        {
            Name = NodeName,
            // Above the 3D render, below the HUD on layer 1, so the overlay
            // never dims the readouts a player needs during a fight.
            Layer = 0,
        };

        var image = Image.CreateEmpty(
            GradientWidth, GradientHeight, false, Image.Format.Rgba8);
        for (var y = 0; y < GradientHeight; y++)
        {
            var alpha = AlphaAt((float)y / (GradientHeight - 1));
            for (var x = 0; x < GradientWidth; x++)
            {
                image.SetPixel(x, y, new Color(0.0f, 0.0f, 0.0f, alpha));
            }
        }

        var rect = new TextureRect
        {
            Name = "VignetteGradient",
            Texture = ImageTexture.CreateFromImage(image),
            StretchMode = TextureRect.StretchModeEnum.Scale,
            // Must never eat a click, and must follow window resizes.
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        rect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        vignette.AddChild(rect);
        return vignette;
    }
}
