using System.Collections.Generic;
using Godot;
using ProjectMannequin.Core;

namespace ProjectMannequin.Presentation;

public partial class CombatAudioManager : Node
{
    [Export] public NodePath SimulationPath { get; set; } = "../GameSimulation";

    [ExportGroup("Sound Effects (Leave Null for Mods)")]
    [Export] public AudioStream? HitConnectedSound { get; set; }
    [Export] public AudioStream? HeavyHitSound { get; set; }
    [Export] public AudioStream? GuardHitSound { get; set; }
    [Export] public AudioStream? GuardBrokenSound { get; set; }
    [Export] public AudioStream? ParrySound { get; set; }
    [Export] public AudioStream? SuperStartedSound { get; set; }
    [Export] public AudioStream? BeamClashStartedSound { get; set; }
    [Export] public AudioStream? FormSwapSound { get; set; }
    [Export] public AudioStream? WallBreakSound { get; set; }
    [Export] public AudioStream? BossPhaseChangeSound { get; set; }
    [Export] public AudioStream? IntroSlamSound { get; set; }
    [Export] public AudioStream? AnnouncerReadySound { get; set; }
    [Export] public AudioStream? AnnouncerFightSound { get; set; }
    [Export] public AudioStream? GongSound { get; set; }

    private GameSimulation? _simulation;
    private long _presentationEventCursor;
    private readonly List<CombatPresentationEvent> _presentationEventBuffer = new();
    private readonly HashSet<string> _seenEventKeys = new();
    private readonly Queue<AudioStreamPlayer> _pool = new();
    private readonly List<AudioStream> _ownedProceduralStreams = new();

    public override void _Ready()
    {
        _simulation = GetNodeOrNull<GameSimulation>(SimulationPath);
        for (int i = 0; i < 16; i++)
        {
            var player = new AudioStreamPlayer { Bus = "SFX" };
            AddChild(player);
            _pool.Enqueue(player);
        }

        // Procedural announcer stingers so the ROUND 1 / FIGHT cues are audible
        // without external audio assets (designers can still assign real VO).
        // Procedural boss-intro cues so the cinematic slam / READY / FIGHT beats
        // are audible without external audio assets (designers can still assign
        // real voice-over on these exports).
        IntroSlamSound ??= Own(ProceduralAudioFactory.CreateSlam());
        AnnouncerReadySound ??= Own(ProceduralAudioFactory.CreateStinger(new[] { 262.0f, 330.0f, 392.0f }, 0.34f, 0.55f));
        AnnouncerFightSound ??= Own(ProceduralAudioFactory.CreateStinger(new[] { 392.0f, 523.0f, 784.0f }, 0.38f, 0.6f));
        GongSound ??= Own(ProceduralAudioFactory.CreateGong());
        HitConnectedSound ??= Own(ProceduralAudioFactory.CreateImpact(185.0f, 0.095f, 0.34f, 0.48f, 1101));
        HeavyHitSound ??= Own(ProceduralAudioFactory.CreateImpact(92.0f, 0.16f, 0.52f, 0.68f, 1102));
        GuardHitSound ??= Own(ProceduralAudioFactory.CreateImpact(340.0f, 0.09f, 0.22f, 0.42f, 1103));
        GuardBrokenSound ??= Own(ProceduralAudioFactory.CreateSlam(0.34f, 0.68f));
        ParrySound ??= Own(ProceduralAudioFactory.CreateStinger(new[] { 660.0f, 990.0f, 1320.0f }, 0.16f, 0.48f));
        SuperStartedSound ??= Own(ProceduralAudioFactory.CreateSlam(0.42f, 0.72f));
        BeamClashStartedSound ??= Own(ProceduralAudioFactory.CreateGong(146.0f, 0.72f, 0.55f));
        FormSwapSound ??= Own(ProceduralAudioFactory.CreateStinger(new[] { 220.0f, 440.0f, 880.0f }, 0.28f, 0.46f));
        WallBreakSound ??= Own(ProceduralAudioFactory.CreateSlam(0.62f, 0.82f));
        BossPhaseChangeSound ??= Own(ProceduralAudioFactory.CreateStinger(new[] { 110.0f, 165.0f, 247.0f }, 0.48f, 0.62f));
    }

    public override void _ExitTree()
    {
        foreach (var child in GetChildren())
        {
            if (child is not AudioStreamPlayer player)
            {
                continue;
            }

            player.Stop();
            player.Stream = null;
        }

        _pool.Clear();
        IntroSlamSound = null;
        AnnouncerReadySound = null;
        AnnouncerFightSound = null;
        GongSound = null;
        HitConnectedSound = null;
        HeavyHitSound = null;
        GuardHitSound = null;
        GuardBrokenSound = null;
        ParrySound = null;
        SuperStartedSound = null;
        BeamClashStartedSound = null;
        FormSwapSound = null;
        WallBreakSound = null;
        BossPhaseChangeSound = null;
        foreach (var stream in _ownedProceduralStreams)
        {
            stream.Dispose();
        }

        _ownedProceduralStreams.Clear();
    }

    private AudioStreamWav Own(AudioStreamWav stream)
    {
        _ownedProceduralStreams.Add(stream);
        return stream;
    }

    public override void _Process(double delta)
    {
        if (_simulation is null)
        {
            return;
        }

        _presentationEventBuffer.Clear();
        _presentationEventCursor = _simulation.CopyPresentationEventsSince(
            _presentationEventCursor,
            _presentationEventBuffer);
        foreach (var evt in _presentationEventBuffer)
        {
            var key = $"{evt.Tick}:{evt.Type}:{evt.SourceActorId}:{evt.TargetActorId}:{evt.Payload}";
            if (!_seenEventKeys.Add(key))
            {
                continue;
            }
            if (_seenEventKeys.Count > 128)
            {
                _seenEventKeys.Clear();
            }

            HandlePresentationEvent(evt);
        }
    }

    private void HandlePresentationEvent(CombatPresentationEvent evt)
    {
        switch (evt.Type)
        {
            case CombatPresentationEventType.HitConnected:
            case CombatPresentationEventType.CounterHit:
            case CombatPresentationEventType.PunishCounter:
                var damage = ParseDamage(evt.Payload);
                PlaySound(
                    damage > 1500 ? HeavyHitSound : HitConnectedSound,
                    ResolveImpactPitch(evt, damage > 1500 ? 0.94f : 1.0f));
                break;
            case CombatPresentationEventType.LauncherHit:
                PlaySound(HeavyHitSound, ResolveImpactPitch(evt, 0.96f));
                break;
            case CombatPresentationEventType.Blocked:
                PlaySound(GuardHitSound);
                break;
            case CombatPresentationEventType.GuardBroken:
                PlaySound(GuardBrokenSound);
                break;
            case CombatPresentationEventType.Parried:
                PlaySound(ParrySound);
                break;
            case CombatPresentationEventType.SuperStarted:
                PlaySound(SuperStartedSound);
                break;
            case CombatPresentationEventType.BeamClashStarted:
                PlaySound(BeamClashStartedSound);
                break;
            case CombatPresentationEventType.FormSwapCompleted:
                PlaySound(FormSwapSound);
                break;
            case CombatPresentationEventType.WallBreakStarted:
                PlaySound(WallBreakSound);
                break;
            case CombatPresentationEventType.PropExploded:
                PlaySound(WallBreakSound, 1.08f);
                break;
            case CombatPresentationEventType.BossPhaseChanged:
                PlaySound(BossPhaseChangeSound);
                break;
            case CombatPresentationEventType.BossIntroStarted:
                PlaySound(IntroSlamSound);
                break;
            case CombatPresentationEventType.BossIntroReady:
                PlaySound(AnnouncerReadySound);
                break;
            case CombatPresentationEventType.BossIntroFight:
                PlaySound(AnnouncerFightSound);
                PlaySound(GongSound);
                break;
            case CombatPresentationEventType.EliteIntroStarted:
                PlaySound(IntroSlamSound, 1.08f);
                break;
            case CombatPresentationEventType.EliteIntroReady:
                PlaySound(AnnouncerReadySound, 1.08f);
                break;
            case CombatPresentationEventType.EliteIntroFight:
                PlaySound(AnnouncerFightSound, 1.12f);
                break;
        }
    }

    private void PlaySound(AudioStream? stream, float pitchScale = 1.0f)
    {
        if (stream is null || _pool.Count == 0)
        {
            return;
        }

        var player = _pool.Dequeue();
        player.Stream = stream;
        player.PitchScale = Mathf.Clamp(pitchScale, 0.82f, 1.18f);
        player.Play();
        
        // Use a signal connection for when it finishes playing to enqueue it back
        var callable = Callable.From(() => _pool.Enqueue(player));
        player.Connect(AudioStreamPlayer.SignalName.Finished, callable, (uint)ConnectFlags.OneShot);
    }

    private static int ParseDamage(string payload)
    {
        var separatorIndex = payload.LastIndexOf('|');
        return separatorIndex >= 0
               && separatorIndex < payload.Length - 1
               && int.TryParse(payload[(separatorIndex + 1)..], out var damage)
            ? damage
            : 0;
    }

    private static float ResolveImpactPitch(CombatPresentationEvent evt, float center)
    {
        // Presentation variation is derived from authoritative event data rather
        // than random state, so captures/replays retain the same sound cadence.
        var variationStep = (evt.Tick + evt.SourceActorId.Length + evt.TargetActorId.Length) % 5;
        return center + (variationStep - 2) * 0.025f;
    }
}

