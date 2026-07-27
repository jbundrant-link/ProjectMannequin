using Godot;

namespace ProjectMannequin.Progression;

/// <summary>
/// How a save file's schema version relates to the version this build writes.
/// </summary>
public enum SaveCompatibility
{
    /// <summary>Exactly the version this build writes.</summary>
    Current,

    /// <summary>Older, but within the range this build can migrate forward.</summary>
    Migrated,

    /// <summary>Newer than this build understands. Must be treated as read-only.</summary>
    FutureVersion,

    /// <summary>Too old to migrate, or a nonsense version. Must not be trusted.</summary>
    Unsupported,
}

/// <summary>
/// Shared schema-version policy for the persistent save stores.
/// </summary>
/// <remarks>
/// Pure and parameterised so the deterministic suite can cover every branch
/// without touching disk. It exists because the two stores previously handled
/// version mismatch in opposite and equally wrong ways: the progression store
/// blindly restamped any file to the current version, and the run store threw
/// away any file whose version was not an exact match.
/// </remarks>
public static class SaveSchema
{
    public static SaveCompatibility Evaluate(
        int fileVersion,
        int currentVersion,
        int minimumSupportedVersion)
    {
        if (fileVersion == currentVersion)
        {
            return SaveCompatibility.Current;
        }

        // A newer build wrote this file. Deserializing drops the fields this
        // build does not know about, so writing it back would silently delete
        // the player's newer data. Read it, never overwrite it.
        if (fileVersion > currentVersion)
        {
            return SaveCompatibility.FutureVersion;
        }

        if (fileVersion < minimumSupportedVersion || fileVersion < 1)
        {
            return SaveCompatibility.Unsupported;
        }

        return SaveCompatibility.Migrated;
    }

    /// <summary>
    /// Whether this build may write over a file that reported the given
    /// compatibility.
    /// </summary>
    public static bool MayOverwrite(SaveCompatibility compatibility) =>
        compatibility is SaveCompatibility.Current or SaveCompatibility.Migrated;

    /// <summary>
    /// Wall-clock milliseconds of the last successful write to any save file,
    /// or null if nothing has been written this session.
    /// </summary>
    /// <remarks>
    /// Deliberately not simulation state: the save indicator is presentation
    /// only and must never affect a replay.
    /// </remarks>
    public static ulong? LastSaveMilliseconds { get; private set; }

    public static void NotifySaved() => LastSaveMilliseconds = Time.GetTicksMsec();

    /// <summary>
    /// Whether the brief "saved" indicator should currently be shown.
    /// </summary>
    public static bool ShouldShowSaveIndicator(
        ulong nowMilliseconds,
        ulong holdMilliseconds = 1800UL) =>
        ShouldShowSaveIndicator(LastSaveMilliseconds, nowMilliseconds, holdMilliseconds);

    /// <summary>
    /// Pure form, so the deterministic suite can drive the clock instead of
    /// depending on whether anything happened to save this session.
    /// </summary>
    public static bool ShouldShowSaveIndicator(
        ulong? savedAtMilliseconds,
        ulong nowMilliseconds,
        ulong holdMilliseconds)
    {
        // A now earlier than the save means the clock moved backwards; show
        // nothing rather than wrapping the unsigned subtraction into a huge
        // value that would pin the indicator on forever.
        if (savedAtMilliseconds is not { } saved || nowMilliseconds < saved)
        {
            return false;
        }

        return nowMilliseconds - saved < holdMilliseconds;
    }

    public static void WarnFutureSave(string path, int fileVersion, int currentVersion)
    {
        GD.PushWarning(
            $"Save '{path}' is schema v{fileVersion} but this build writes v{currentVersion}. "
            + "Loading it read-only; it will not be overwritten so a newer build's data survives.");
    }
}
