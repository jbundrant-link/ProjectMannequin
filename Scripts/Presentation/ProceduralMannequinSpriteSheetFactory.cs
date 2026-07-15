using Godot;

namespace ProjectMannequin.Presentation;

public static class ProceduralMannequinSpriteSheetFactory
{
    public const int Columns = 10;
    public const int Rows = 7;
    public const int FrameWidth = 96;
    public const int FrameHeight = 128;

    public static Texture2D Create(MannequinSpritePalette palette)
    {
        var image = Image.CreateEmpty(Columns * FrameWidth, Rows * FrameHeight, false, Image.Format.Rgba8);
        image.Fill(Colors.Transparent);

        for (var column = 0; column < Columns; column++)
        {
            DrawFrame(image, column, 0, PoseKind.Idle, column / (float)Columns, palette);
            DrawFrame(image, column, 1, PoseKind.Walk, column / (float)Columns, palette);
            DrawFrame(image, column, 2, PoseKind.Dash, column / (float)Columns, palette);
            DrawFrame(image, column, 3, PoseKind.Jump, column / (float)Columns, palette);
            DrawFrame(image, column, 4, PoseKind.Crouch, column / (float)Columns, palette);
            DrawFrame(image, column, 5, PoseKind.Attack, column / (float)Columns, palette);
            DrawFrame(image, column, 6, column < 3 ? PoseKind.Hit : column < 7 ? PoseKind.FormSwap : PoseKind.Dead, column / (float)Columns, palette);
        }

        return ImageTexture.CreateFromImage(image);
    }

    private static void DrawFrame(
        Image image,
        int column,
        int row,
        PoseKind pose,
        float t,
        MannequinSpritePalette palette)
    {
        var originX = column * FrameWidth;
        var originY = row * FrameHeight;
        var bob = pose switch
        {
            PoseKind.Idle => Mathf.RoundToInt(Mathf.Sin(t * Mathf.Tau) * 1.5f),
            PoseKind.Walk => Mathf.RoundToInt(Mathf.Abs(Mathf.Sin(t * Mathf.Tau)) * -3.0f),
            PoseKind.Dash => Mathf.RoundToInt(Mathf.Abs(Mathf.Sin(t * Mathf.Tau)) * -5.0f),
            PoseKind.Crouch => -6,
            PoseKind.FormSwap => Mathf.RoundToInt(Mathf.Sin(t * Mathf.Tau * 2.0f) * 2.0f),
            _ => 0,
        };

        var lean = pose switch
        {
            PoseKind.Dash => 5,
            PoseKind.Attack => 4,
            PoseKind.Hit => -4,
            _ => 0,
        };

        var swing = Mathf.Sin(t * Mathf.Tau);
        var attackStep = AttackStep(t);

        var head = new Vector2I(43 + lean, 10 + bob);
        var chest = new Vector2I(37 + lean, 35 + bob);
        var abdomen = new Vector2I(40 + lean, 58 + bob);
        var pelvis = new Vector2I(36 + lean, 75 + bob);

        DrawSegment(image, originX + head.X, originY + head.Y, 18, 22, palette.Light, palette.Outline);
        DrawSegment(image, originX + head.X + 11, originY + head.Y + 5, 4, 9, palette.Accent, palette.Outline);
        DrawSegment(image, originX + chest.X, originY + chest.Y, 28, 22, palette.Base, palette.Outline);
        DrawSegment(image, originX + chest.X + 15, originY + chest.Y + 4, 8, 14, palette.Light, palette.Base);
        DrawSegment(image, originX + abdomen.X, originY + abdomen.Y, 22, 18, palette.Base, palette.Outline);
        DrawSegment(image, originX + pelvis.X, originY + pelvis.Y, 30, 14, palette.Shadow, palette.Outline);

        DrawArm(image, originX, originY, left: true, pose, swing, attackStep, bob, lean, palette);
        DrawArm(image, originX, originY, left: false, pose, -swing, attackStep, bob, lean, palette);
        DrawLeg(image, originX, originY, left: true, pose, -swing, bob, lean, palette);
        DrawLeg(image, originX, originY, left: false, pose, swing, bob, lean, palette);

        if (pose == PoseKind.FormSwap)
        {
            var pulse = Mathf.RoundToInt(4.0f + Mathf.Sin(t * Mathf.Tau * 2.0f) * 2.0f);
            DrawRect(image, originX + 30 - pulse, originY + 22 - pulse, 36 + pulse * 2, 70 + pulse * 2, palette.Accent with { A = 0.20f });
        }

        if (pose == PoseKind.Dead)
        {
            DrawRect(image, originX + 22, originY + 113, 54, 4, palette.Shadow);
        }
    }

    private static void DrawArm(
        Image image,
        int originX,
        int originY,
        bool left,
        PoseKind pose,
        float swing,
        int attackStep,
        int bob,
        int lean,
        MannequinSpritePalette palette)
    {
        var side = left ? -1 : 1;
        var shoulderX = 48 + lean + side * 18;
        var shoulderY = 40 + bob;
        var armSwing = Mathf.RoundToInt(swing * 5.0f);
        var attackReach = !left && pose == PoseKind.Attack ? attackStep : 0;
        var guardPull = left && pose == PoseKind.Attack ? -5 : 0;

        DrawSegment(image, originX + shoulderX - 4, originY + shoulderY - 4, 8, 8, palette.Joint, palette.Outline);
        DrawSegment(image, originX + shoulderX + side * (2 + attackReach + guardPull), originY + shoulderY + 3 + armSwing, 10, 22, palette.Base, palette.Outline);
        DrawSegment(image, originX + shoulderX + side * (4 + attackReach * 2 + guardPull), originY + shoulderY + 23 - armSwing / 2, 9, 20, palette.Base, palette.Outline);
        DrawSegment(image, originX + shoulderX + side * (5 + attackReach * 3 + guardPull), originY + shoulderY + 43 - armSwing / 2, 12, 10, palette.Shadow, palette.Outline);
    }

    private static void DrawLeg(
        Image image,
        int originX,
        int originY,
        bool left,
        PoseKind pose,
        float swing,
        int bob,
        int lean,
        MannequinSpritePalette palette)
    {
        var side = left ? -1 : 1;
        var hipX = 48 + lean + side * 9;
        var hipY = 83 + bob;
        var stride = pose is PoseKind.Walk or PoseKind.Dash ? Mathf.RoundToInt(swing * (pose == PoseKind.Dash ? 10.0f : 7.0f)) : 0;
        var lift = pose is PoseKind.Walk or PoseKind.Dash ? Mathf.RoundToInt(Mathf.Max(0.0f, swing) * -5.0f) : 0;

        if (pose == PoseKind.Jump)
        {
            stride += left ? -5 : 4;
            lift += left ? -8 : -2;
        }

        DrawSegment(image, originX + hipX - 4, originY + hipY - 3, 8, 8, palette.Joint, palette.Outline);
        DrawSegment(image, originX + hipX - 5 + stride / 2, originY + hipY + 3 + lift, 10, 24, palette.Base, palette.Outline);
        DrawSegment(image, originX + hipX - 4 + stride, originY + hipY + 25 + lift / 2, 9, 24, palette.Base, palette.Outline);
        DrawSegment(image, originX + hipX - 7 + stride + side * 3, originY + hipY + 49 + lift / 2, 18, 7, palette.Shadow, palette.Outline);
    }

    private static int AttackStep(float t)
    {
        if (t < 0.25f)
        {
            return 0;
        }

        if (t < 0.55f)
        {
            return 8;
        }

        if (t < 0.78f)
        {
            return 13;
        }

        return 4;
    }

    private static void DrawSegment(Image image, int x, int y, int width, int height, Color fill, Color outline)
    {
        DrawRect(image, x - 2, y - 2, width + 4, height + 4, outline);
        DrawRect(image, x, y, width, height, fill);
        DrawRect(image, x + 2, y + 2, Mathf.Max(2, width / 3), Mathf.Max(2, height / 3), fill.Lightened(0.18f));
        DrawRect(image, x + width - 4, y + height - 4, 3, 3, fill.Darkened(0.25f));
    }

    private static void DrawRect(Image image, int x, int y, int width, int height, Color color)
    {
        var clampedX = Mathf.Clamp(x, 0, image.GetWidth() - 1);
        var clampedY = Mathf.Clamp(y, 0, image.GetHeight() - 1);
        var clampedWidth = Mathf.Clamp(width, 0, image.GetWidth() - clampedX);
        var clampedHeight = Mathf.Clamp(height, 0, image.GetHeight() - clampedY);

        if (clampedWidth <= 0 || clampedHeight <= 0)
        {
            return;
        }

        image.FillRect(new Rect2I(clampedX, clampedY, clampedWidth, clampedHeight), color);
    }

    private enum PoseKind
    {
        Idle,
        Walk,
        Dash,
        Jump,
        Crouch,
        Attack,
        Hit,
        FormSwap,
        Dead,
    }
}

public readonly record struct MannequinSpritePalette(
    Color Outline,
    Color Base,
    Color Light,
    Color Shadow,
    Color Joint,
    Color Accent);
