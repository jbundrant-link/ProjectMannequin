using ProjectMannequin.Core;

namespace ProjectMannequin.LocalInput;

public readonly record struct InputFrame(
    int Tick,
    InputButtons Held,
    InputButtons Pressed,
    InputButtons Released)
{
    public static readonly InputFrame Empty = new(-1, InputButtons.None, InputButtons.None, InputButtons.None);
}

public sealed class PlayerInputBuffer
{
    private readonly InputFrame[] _frames = new InputFrame[GameConstants.InputBufferSize];
    private int _cursor = -1;
    private int _count;
    private InputButtons _lastHeld;

    public int Count => _count;

    public InputButtons CurrentHeld => GetFrame(0).Held;
    public InputButtons JustPressed => GetFrame(0).Pressed;
    public InputButtons JustReleased => GetFrame(0).Released;

    public void PushFrame(int tick, InputButtons held)
    {
        var pressed = held & ~_lastHeld;
        var released = _lastHeld & ~held;

        _cursor = (_cursor + 1) % _frames.Length;
        _frames[_cursor] = new InputFrame(tick, held, pressed, released);
        _lastHeld = held;

        if (_count < _frames.Length)
        {
            _count++;
        }
    }

    public InputFrame GetFrame(int framesBack)
    {
        if (framesBack < 0 || framesBack >= _count || _cursor < 0)
        {
            return InputFrame.Empty;
        }

        var index = (_cursor - framesBack + _frames.Length) % _frames.Length;
        return _frames[index];
    }

    public bool WasJustPressed(InputButtons buttons, int framesBack = 0)
    {
        return GetFrame(framesBack).Pressed.HasAll(buttons);
    }

    public bool IsHeld(InputButtons buttons, int framesBack = 0)
    {
        return GetFrame(framesBack).Held.HasAll(buttons);
    }

    public bool WasPressedWithin(InputButtons buttons, int maxFramesBack)
    {
        var frameLimit = System.Math.Min(System.Math.Max(0, maxFramesBack), _count - 1);
        for (var framesBack = 0; framesBack <= frameLimit; framesBack++)
        {
            if (WasJustPressed(buttons, framesBack))
            {
                return true;
            }
        }

        return false;
    }

    public bool WasPressedTwiceWithin(InputButtons buttons, int maxFramesBack)
    {
        var frameLimit = System.Math.Min(System.Math.Max(0, maxFramesBack), _count - 1);
        var pressCount = 0;
        for (var framesBack = 0; framesBack <= frameLimit; framesBack++)
        {
            if (GetFrame(framesBack).Pressed.HasAny(buttons))
            {
                pressCount++;
                if (pressCount >= 2)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public int GetNumericDirection(int framesBack, bool facingRight)
    {
        var held = GetFrame(framesBack).Held;
        return ToNumericDirection(held, facingRight);
    }

    public static int ToNumericDirection(InputButtons held, bool facingRight)
    {
        var vertical = 0;
        // Motion notation uses the authored W/S and gamepad directional axis.
        // The dedicated Crouch button is deliberately excluded here:
        // CommandInterpreter handles single down-direction crouching normals
        // separately so S+LP remains standing LP while C+LP becomes 2LP.
        if (held.HasAny(InputButtons.Down))
        {
            vertical = -1;
        }
        else if (held.HasAny(InputButtons.Up))
        {
            vertical = 1;
        }

        var horizontal = 0;
        if (held.HasAny(InputButtons.Right))
        {
            horizontal = facingRight ? 1 : -1;
        }
        else if (held.HasAny(InputButtons.Left))
        {
            horizontal = facingRight ? -1 : 1;
        }

        return (horizontal, vertical) switch
        {
            (-1, -1) => 1,
            (0, -1) => 2,
            (1, -1) => 3,
            (-1, 0) => 4,
            (0, 0) => 5,
            (1, 0) => 6,
            (-1, 1) => 7,
            (0, 1) => 8,
            (1, 1) => 9,
            _ => 5,
        };
    }
}
