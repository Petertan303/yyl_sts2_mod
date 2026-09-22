using System;
using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using yyl_sts2_mod.Code.Stances.Vfx;
using yyl_sts2_mod.Code.Utils;
using yyl_sts2_mod.Code.Vfx;

namespace yyl_sts2_mod.Code.Stances;

public class StanceVfxController(StanceVfxConfig cfg)
{
    private const float AmbienceFadeTime = 0.6f;
    private Node2D? _vfxInstance;
    private string? _ambiencePath;

    public async Task OnEnter(Creature owner)
    {
        // VFX 失败绝不允许冒泡到 SetStance / 卡牌 OnPlay，否则会破坏战斗指令队列（表现为下一战斗首回合卡死）。
        try
        {
            await CreateAura(owner);
            PlayEnterSfx();
            StartAmbience();
            if (LocalContext.IsMe(owner))
            {
                PlayScreenFlash();
                PlayScreenShake();
            }
        }
        catch (Exception e)
        {
            GD.PushError($"[yyl] StanceVfxController.OnEnter failed (VFX skipped, combat safe): {e}");
        }
    }

    public async Task OnExit(Creature owner)
    {
        try
        {
            RemoveAura();
            StopAmbience();
        }
        catch (Exception e)
        {
            GD.PushError($"[yyl] StanceVfxController.OnExit failed: {e}");
        }
        await Task.CompletedTask;
    }

    // ── Aura ──────────────────────────────────────

    private Task CreateAura(Creature owner)
    {
        try
        {
            if (cfg.AuraScenePath == null || !ResourceLoader.Exists(cfg.AuraScenePath))
                return Task.CompletedTask;

            var visuals = NCombatRoom.Instance?.GetCreatureNode(owner)?.Visuals;
            if (visuals == null) return Task.CompletedTask;

            var container = visuals.GetNodeOrNull<Node2D>("StanceVfxContainer")
                            ?? CreateContainer(visuals);

            if (_vfxInstance != null && GodotObject.IsInstanceValid(_vfxInstance))
                _vfxInstance.QueueFree();

            var packedScene = ResourceLoader.Load<PackedScene>(cfg.AuraScenePath);
            if (packedScene == null) return Task.CompletedTask;
            _vfxInstance = packedScene.Instantiate<Node2D>();
            _vfxInstance.Position = Vector2.Zero;
            _vfxInstance.Scale = Vector2.One;
            container.AddChild(_vfxInstance);

            foreach (var burst in _vfxInstance.GetChildren()
                         .Where(c => c.Name.ToString().Contains("Burst"))
                         .Cast<Node2D>())
            {
                var pos = burst.GlobalPosition;
                burst.Reparent(visuals);
                burst.GlobalPosition = pos;
                visuals.MoveChild(burst, 0);
            }
        }
        catch (Exception e)
        {
            GD.PushError($"[yyl] StanceVfxController.CreateAura failed: {e}");
        }

        return Task.CompletedTask;
    }

    private static Node2D CreateContainer(Node visuals)
    {
        var c = new Node2D { Name = "StanceVfxContainer", Position = Vector2.Zero };
        visuals.AddChild(c);
        return c;
    }

    private void RemoveAura()
    {
        if (_vfxInstance == null || !GodotObject.IsInstanceValid(_vfxInstance)) return;

        foreach (var child in _vfxInstance.GetChildren())
            switch (child)
            {
                case WrathGlowSparkSpawner sparks: sparks.StopSpawning(); break;
                case CalmFrostStreakSpawner streaks: streaks.StopSpawning(); break;
                case DivinityEyeSpawner eyes: eyes.StopSpawning(); break;
                case AuraBlobEmitter blob:
                    foreach (var cpu in blob.GetChildren().OfType<CpuParticles2D>())
                        cpu.Emitting = false;
                    var timer = blob.GetTree().CreateTimer(2.5f);
                    timer.Timeout += () =>
                    {
                        if (GodotObject.IsInstanceValid(blob)) blob.QueueFree();
                    };
                    break;
            }

        _vfxInstance = null;
    }

    // ── SFX ───────────────────────────────────────

    private void PlayEnterSfx()
    {
        if (cfg.EnterSfxPath != null)
            yylAudio.PlaySfx(cfg.EnterSfxPath, 0.8f);
    }

    private void PlayScreenFlash()
    {
        if (cfg.ScreenFlashColor != null)
            ScreenFlashEffect.Play(cfg.ScreenFlashColor.Value);
    }

    private void PlayScreenShake()
    {
        if (cfg.ScreenShakeStrength != ShakeStrength.None)
            NGame.Instance?.ScreenShake(cfg.ScreenShakeStrength, ShakeDuration.Short);
    }

    // ── Ambience ──────────────────────────────────

    private void StartAmbience()
    {
        if (cfg.AmbienceLoopPath == null) return;
        _ambiencePath = cfg.AmbienceLoopPath;
        yylAudio.PlayLooped(_ambiencePath, 0.45f);
    }

    private void StopAmbience()
    {
        if (_ambiencePath == null) return;
        yylAudio.Stop(_ambiencePath, AmbienceFadeTime);
        _ambiencePath = null;
    }
}
