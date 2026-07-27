using System;
using System.Collections.Generic;
using System.Linq;
using ProjectMannequin.Core;

namespace ProjectMannequin.LocalInput;

public static class LocalInputAssignmentPolicy
{
    public static int ResolvePreferredP1Device(
        string deviceKind,
        string joyGuid,
        IReadOnlyList<LocalInputDeviceOption> availableDevices)
    {
        if (!string.Equals(deviceKind, "gamepad", StringComparison.OrdinalIgnoreCase))
        {
            return GameConstants.KeyboardDeviceId;
        }

        if (!string.IsNullOrWhiteSpace(joyGuid))
        {
            foreach (var device in availableDevices)
            {
                if (device.Kind == "gamepad"
                    && string.Equals(device.Guid, joyGuid, StringComparison.OrdinalIgnoreCase))
                {
                    return device.DeviceId;
                }
            }
        }

        foreach (var device in availableDevices)
        {
            if (device.Kind == "gamepad")
            {
                return device.DeviceId;
            }
        }

        return GameConstants.KeyboardDeviceId;
    }

    public static int[] BuildAssignments(
        int playerCount,
        IReadOnlyList<int> connectedJoypads,
        int preferredP1Device)
    {
        var assignments = Enumerable.Repeat(int.MinValue, playerCount).ToArray();
        if (assignments.Length == 0)
        {
            return assignments;
        }

        assignments[0] = preferredP1Device == GameConstants.KeyboardDeviceId
            || connectedJoypads.Contains(preferredP1Device)
                ? preferredP1Device
                : GameConstants.KeyboardDeviceId;

        var nextSlot = 1;
        foreach (var deviceId in connectedJoypads)
        {
            if (nextSlot >= assignments.Length)
            {
                break;
            }

            if (deviceId == assignments[0])
            {
                continue;
            }

            assignments[nextSlot++] = deviceId;
        }

        return assignments;
    }

    public static bool CanAssign(
        IReadOnlyList<int> assignments,
        int playerId,
        int deviceId,
        IReadOnlyList<int> connectedJoypads)
    {
        if (playerId < 1 || playerId > assignments.Count)
        {
            return false;
        }

        if (deviceId != GameConstants.KeyboardDeviceId
            && !connectedJoypads.Contains(deviceId))
        {
            return false;
        }

        return !assignments.Where((_, slot) => slot != playerId - 1)
            .Contains(deviceId);
    }

    public static bool ShouldRefresh(
        bool replayActive,
        IReadOnlyList<int> assignments,
        IReadOnlyList<int> connectedJoypads,
        int preferredP1Device)
    {
        if (replayActive || assignments.Count == 0)
        {
            return false;
        }

        if (assignments[0] != preferredP1Device)
        {
            return true;
        }

        return assignments.Any(deviceId =>
            deviceId != int.MinValue
            && deviceId != GameConstants.KeyboardDeviceId
            && !connectedJoypads.Contains(deviceId));
    }
}