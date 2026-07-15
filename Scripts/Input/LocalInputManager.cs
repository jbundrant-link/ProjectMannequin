using System.Linq;
using Godot;
using ProjectMannequin.Core;

namespace ProjectMannequin.LocalInput;

public partial class LocalInputManager : Node
{
    private readonly PlayerInputBuffer[] _buffers = new PlayerInputBuffer[GameConstants.MaxPlayers];
    private readonly int[] _assignedDevices = Enumerable.Repeat(int.MinValue, GameConstants.MaxPlayers).ToArray();
    private ReplayInputSource? _replaySource;
    private int _lastAssignmentRefreshTick = -GameConstants.TickRate;

    public static LocalInputManager? Active { get; private set; }

    public override void _Ready()
    {
        Active = this;
        for (var index = 0; index < _buffers.Length; index++)
        {
            _buffers[index] = new PlayerInputBuffer();
        }

        EnsureDefaultKeyboardActions();
        AssignDefaultDevices();
    }

    public override void _ExitTree()
    {
        if (Active == this)
        {
            Active = null;
        }
    }

    /// <summary>
    /// Overrides live device polling with recorded inputs for deterministic
    /// replay playback. Pass <c>null</c> to return to live input.
    /// </summary>
    public void SetReplaySource(ReplayInputSource? source)
    {
        _replaySource = source;
    }

    public void PollInputs(int simulationTick)
    {
        if (_replaySource is null
            && (simulationTick - _lastAssignmentRefreshTick >= GameConstants.TickRate
                || simulationTick < _lastAssignmentRefreshTick))
        {
            RefreshDeviceAssignments();
            _lastAssignmentRefreshTick = simulationTick;
        }

        for (var slot = 0; slot < GameConstants.MaxPlayers; slot++)
        {
            var replayDriven = _replaySource is not null && slot < _replaySource.PlayerCount;
            if (_assignedDevices[slot] == int.MinValue && !replayDriven)
            {
                continue;
            }

            var combatSmokeInput = OS.GetEnvironment("PROJECT_MANNEQUIN_COMBAT_SMOKE_TEST") == "1";
            var visualPoseInput = OS.GetEnvironment("PROJECT_MANNEQUIN_VISUAL_POSE_TEST") == "1";
            var mask = replayDriven
                ? _replaySource!.HeldFor(simulationTick, slot + 1)
                : visualPoseInput && slot == 0
                    ? PollVisualPoseInput(simulationTick)
                    : combatSmokeInput && slot == 0
                        ? PollCombatSmokeInput(simulationTick)
                    : _assignedDevices[slot] == GameConstants.KeyboardDeviceId
                        ? PollKeyboard(slot + 1)
                        : PollJoypad(_assignedDevices[slot]);

            _buffers[slot].PushFrame(simulationTick, mask);
        }
    }

    public PlayerInputBuffer? GetBufferForPlayer(int playerId)
    {
        if (playerId < 1 || playerId > GameConstants.MaxPlayers)
        {
            return null;
        }

        return _buffers[playerId - 1];
    }

    public int GetAssignedDevice(int playerId)
    {
        if (playerId < 1 || playerId > GameConstants.MaxPlayers)
        {
            return int.MinValue;
        }

        return _assignedDevices[playerId - 1];
    }

    public bool TrySetAssignedDevice(int playerId, int deviceId, bool persistP1Preference = true)
    {
        if (playerId < 1 || playerId > GameConstants.MaxPlayers)
        {
            return false;
        }

        if (deviceId != GameConstants.KeyboardDeviceId
            && !Godot.Input.GetConnectedJoypads().Contains(deviceId))
        {
            return false;
        }

        for (var slot = 0; slot < _assignedDevices.Length; slot++)
        {
            if (slot != playerId - 1 && _assignedDevices[slot] == deviceId)
            {
                return false;
            }
        }

        _assignedDevices[playerId - 1] = deviceId;
        if (playerId == 1 && persistP1Preference)
        {
            InputDevicePreferences.SelectP1Device(deviceId);
        }

        return true;
    }

    private void AssignDefaultDevices()
    {
        for (var slot = 0; slot < _assignedDevices.Length; slot++)
        {
            _assignedDevices[slot] = int.MinValue;
        }

        var playerOneDevice = InputDevicePreferences.ResolveP1Device();
        _assignedDevices[0] = playerOneDevice;

        var joypads = Godot.Input.GetConnectedJoypads();
        var nextPlayerSlot = 1;
        for (var index = 0; index < joypads.Count && nextPlayerSlot < GameConstants.MaxPlayers; index++)
        {
            if (joypads[index] == playerOneDevice)
            {
                continue;
            }

            _assignedDevices[nextPlayerSlot++] = joypads[index];
        }
    }

    private void RefreshDeviceAssignments()
    {
        var connected = Godot.Input.GetConnectedJoypads();
        var desiredPlayerOneDevice = InputDevicePreferences.ResolveP1Device();
        var assignmentInvalid = _assignedDevices[0] != desiredPlayerOneDevice;
        for (var slot = 0; slot < _assignedDevices.Length && !assignmentInvalid; slot++)
        {
            var assigned = _assignedDevices[slot];
            assignmentInvalid = assigned != int.MinValue
                && assigned != GameConstants.KeyboardDeviceId
                && !connected.Contains(assigned);
        }

        if (assignmentInvalid)
        {
            AssignDefaultDevices();
        }
    }

    private static InputButtons PollKeyboard(int playerNumber)
    {
        var prefix = $"p{playerNumber}_";
        var mask = InputButtons.None;

        if (Godot.Input.IsActionPressed(prefix + "up")) mask |= InputButtons.Up;
        if (Godot.Input.IsActionPressed(prefix + "down")) mask |= InputButtons.Down;
        if (Godot.Input.IsActionPressed(prefix + "left")) mask |= InputButtons.Left;
        if (Godot.Input.IsActionPressed(prefix + "right")) mask |= InputButtons.Right;
        if (Godot.Input.IsActionPressed(prefix + "crouch")) mask |= InputButtons.Crouch;
        if (Godot.Input.IsActionPressed(prefix + "lp")) mask |= InputButtons.LightPunch;
        if (Godot.Input.IsActionPressed(prefix + "mp")) mask |= InputButtons.MediumPunch;
        if (Godot.Input.IsActionPressed(prefix + "hp")) mask |= InputButtons.HeavyPunch;
        if (Godot.Input.IsActionPressed(prefix + "lk")) mask |= InputButtons.LightKick;
        if (Godot.Input.IsActionPressed(prefix + "mk")) mask |= InputButtons.MediumKick;
        if (Godot.Input.IsActionPressed(prefix + "hk")) mask |= InputButtons.HeavyKick;
        if (Godot.Input.IsActionPressed(prefix + "jump")) mask |= InputButtons.Jump;
        if (Godot.Input.IsActionPressed(prefix + "block")) mask |= InputButtons.Block;
        if (Godot.Input.IsActionPressed(prefix + "grab")) mask |= InputButtons.Grab;
        if (Godot.Input.IsActionPressed(prefix + "dash")) mask |= InputButtons.Dash;
        if (Godot.Input.IsActionPressed(prefix + "form_swap")) mask |= InputButtons.FormSwap;
        if (Godot.Input.IsActionPressed(prefix + "start")) mask |= InputButtons.Start;
        if (Godot.Input.IsActionPressed(prefix + "assist")) mask |= InputButtons.Assist;

        return mask;
    }

    private static InputButtons PollJoypad(int deviceId)
    {
        var mask = InputButtons.None;
        var xAxis = Godot.Input.GetJoyAxis(deviceId, JoyAxis.LeftX);
        var yAxis = Godot.Input.GetJoyAxis(deviceId, JoyAxis.LeftY);

        if (xAxis < -0.45f || Godot.Input.IsJoyButtonPressed(deviceId, JoyButton.DpadLeft)) mask |= InputButtons.Left;
        if (xAxis > 0.45f || Godot.Input.IsJoyButtonPressed(deviceId, JoyButton.DpadRight)) mask |= InputButtons.Right;
        if (yAxis < -0.45f || Godot.Input.IsJoyButtonPressed(deviceId, JoyButton.DpadUp)) mask |= InputButtons.Up;
        if (yAxis > 0.45f) mask |= InputButtons.Down;
        if (Godot.Input.IsJoyButtonPressed(deviceId, JoyButton.DpadDown))
        {
            // D-pad down participates in motions and doubles as the pad's
            // crouch-normal modifier. The analog Y axis remains lane-only.
            mask |= InputButtons.Down | InputButtons.Crouch;
        }

        if (Godot.Input.IsJoyButtonPressed(deviceId, JoyButton.X)) mask |= InputButtons.LightPunch;
        if (Godot.Input.IsJoyButtonPressed(deviceId, JoyButton.Y)) mask |= InputButtons.MediumPunch;
        if (Godot.Input.IsJoyButtonPressed(deviceId, JoyButton.RightShoulder)) mask |= InputButtons.HeavyPunch;
        if (Godot.Input.IsJoyButtonPressed(deviceId, JoyButton.A)) mask |= InputButtons.LightKick;
        if (Godot.Input.IsJoyButtonPressed(deviceId, JoyButton.B)) mask |= InputButtons.MediumKick;
        if (Godot.Input.IsJoyButtonPressed(deviceId, JoyButton.LeftShoulder)) mask |= InputButtons.HeavyKick;
        if (Godot.Input.IsJoyButtonPressed(deviceId, JoyButton.LeftStick)) mask |= InputButtons.Dash;
        if (Godot.Input.IsJoyButtonPressed(deviceId, JoyButton.RightStick)) mask |= InputButtons.FormSwap;
        if (Godot.Input.IsJoyButtonPressed(deviceId, JoyButton.Misc1)) mask |= InputButtons.Jump;
        if (Godot.Input.GetJoyAxis(deviceId, JoyAxis.TriggerLeft) > 0.35f) mask |= InputButtons.Block;
        if (Godot.Input.GetJoyAxis(deviceId, JoyAxis.TriggerRight) > 0.35f) mask |= InputButtons.Grab;
        if (Godot.Input.IsJoyButtonPressed(deviceId, JoyButton.Start)) mask |= InputButtons.Start;
        if (Godot.Input.IsJoyButtonPressed(deviceId, JoyButton.Back)) mask |= InputButtons.Assist;

        return mask;
    }

    private static InputButtons PollCombatSmokeInput(int tick)
    {
        return tick switch
        {
            1 => InputButtons.Right | InputButtons.Dash,
            20 => InputButtons.Jump,
            31 => InputButtons.LightPunch,
            40 => InputButtons.MediumPunch,
            55 => InputButtons.HeavyPunch,
            105 => InputButtons.LightPunch,
            114 => InputButtons.MediumPunch,
            126 => InputButtons.HeavyPunch,
            175 => InputButtons.Crouch | InputButtons.LightKick,
            215 => InputButtons.Crouch | InputButtons.HeavyPunch,
            229 => InputButtons.Jump,
            238 => InputButtons.LightPunch,
            249 => InputButtons.HeavyPunch,
            320 => InputButtons.Right,
            322 => InputButtons.Down,
            324 => InputButtons.Down | InputButtons.Right | InputButtons.HeavyPunch,
            405 => InputButtons.LightPunch,
            410 => InputButtons.MediumPunch,
            416 => InputButtons.Down,
            417 => InputButtons.Down | InputButtons.Right,
            418 => InputButtons.Right,
            419 => InputButtons.Right | InputButtons.MediumPunch,
            _ => InputButtons.None,
        };
    }

    private static InputButtons PollVisualPoseInput(int tick)
    {
        if (tick is >= 30 and <= 95)
        {
            return tick == 60
                ? InputButtons.Crouch | InputButtons.LightPunch
                : InputButtons.Crouch;
        }

        return tick switch
        {
            110 => InputButtons.Jump,
            122 => InputButtons.LightPunch,
            _ => InputButtons.None,
        };
    }

    private static void EnsureDefaultKeyboardActions()
    {
        EnsureKeyAction("p1_up", Key.W);
        EnsureKeyAction("p1_down", Key.S);
        EnsureKeyAction("p1_left", Key.A);
        EnsureKeyAction("p1_right", Key.D);
        EnsureKeyAction("p1_lp", Key.J);
        EnsureKeyAction("p1_mp", Key.K);
        EnsureKeyAction("p1_hp", Key.L);
        EnsureKeyAction("p1_lk", Key.U);
        EnsureKeyAction("p1_mk", Key.I);
        EnsureKeyAction("p1_hk", Key.O);
        EnsureKeyAction("p1_jump", Key.Space);
        EnsureKeyAction("p1_block", Key.B);
        EnsureKeyAction("p1_grab", Key.H);
        EnsureKeyAction("p1_dash", Key.E);
        EnsureKeyAction("p1_crouch", Key.C);
        EnsureKeyAction("p1_form_swap", Key.Q);
        EnsureKeyAction("p1_assist", Key.R);
        EnsureKeyAction("p1_start", Key.Enter);
    }

    private static void EnsureKeyAction(string actionName, Key key)
    {
        var godotAction = new StringName(actionName);
        if (!InputMap.HasAction(godotAction))
        {
            InputMap.AddAction(godotAction);
        }

        var keyEvent = new InputEventKey
        {
            PhysicalKeycode = key,
        };

        InputMap.ActionAddEvent(godotAction, keyEvent);
    }
}
