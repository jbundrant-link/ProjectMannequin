using Godot;
using ProjectMannequin.Core;
using ProjectMannequin.Data;

namespace ProjectMannequin.Combat;

public static class CombatActorFactory
{
    public static CombatActor CreateAndRegister(
        Node3D actorRoot,
        GameSimulation simulation,
        string actorId,
        string displayName,
        CharacterData form,
        Vector3 position,
        int teamId,
        int playerId,
        bool isPlayer,
        bool isBoss,
        Color presentationTint)
    {
        var actor = new CombatActor
        {
            ActorId = actorId,
            Name = displayName,
            TeamId = teamId,
            PlayerId = playerId,
            IsPlayerControlled = isPlayer,
            IsBoss = isBoss,
            SimPosition = position,
            Position = position,
            FacingRight = teamId == 1,
            PresentationTint = presentationTint,
        };

        actorRoot.AddChild(actor);
        actor.Initialize(form);
        simulation.RegisterActor(actor);
        return actor;
    }
}
