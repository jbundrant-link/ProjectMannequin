using System.Collections.Generic;

namespace ProjectMannequin.Data;

/// <summary>
/// Central list of the reference playable and boss forms used by tooling such as
/// the frame-data report and move validation. Covers the roadmap-required forms:
/// mannequin, Archive Knight, Ryu, and Goku (boss and archive variants).
/// </summary>
public static class RosterCatalog
{
    public static IReadOnlyList<CharacterData> ReferenceForms()
    {
        return new List<CharacterData>
        {
            TestRosterFactory.CreateBlankMannequin(),
            TestRosterFactory.CreateTestBoss(),
            TestRosterFactory.CreateArchiveKnightForm(),
            TestRosterFactory.CreateWorldWarriorRyuBoss(),
            TestRosterFactory.CreateWorldWarriorRyuForm(),
            GokuRosterFactory.CreateGokuBoss(),
            GokuRosterFactory.CreateGokuArchiveForm(),
        };
    }
}
