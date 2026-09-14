using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace yyl_sts2_mod.Code.Utils;

/// <summary>帧动画角色统一触发入口；姿态仅使用角色着色，不依赖 Spine 节点。</summary>
public static class yylAnim
{
    public const string Attack = "Attack";
    public const string Cast = "Cast";
    public const string Hit = "Hit";
    public const string Dead = "Dead";

    private static readonly SpireField<Creature, Color?> OriginalTint = new(() => null);

    public static Task TriggerAttack(CardModel card) =>
        TriggerAttack(card.Owner.Creature, card.Owner.Character.AttackAnimDelay);

    public static Task TriggerCast(CardModel card) =>
        TriggerCast(card.Owner.Creature, card.Owner.Character.CastAnimDelay);

    public static Task TriggerAttack(Creature creature, float delay) =>
        TriggerAnim(creature, Attack, "attack", delay);

    public static Task TriggerCast(Creature creature, float delay) =>
        TriggerAnim(creature, Cast, "cast", delay);

    public static Task TriggerHit(Creature creature, float delay = 0.1f) =>
        TriggerAnim(creature, Hit, "hurt", delay);

    public static Task TriggerDead(Creature creature, float delay = 0.1f) =>
        TriggerAnim(creature, Dead, "die", delay);

    public static async Task PlayStanceTransition(Creature creature, Color? tint)
    {
        await TriggerCast(creature, creature.Player?.Character.CastAnimDelay ?? 0.25f);
        if (tint == null) return;

        var sprite = FindSprite(creature);
        if (sprite == null) return;
        OriginalTint[creature] ??= sprite.Modulate;
        sprite.Modulate = tint.Value;
    }

    public static void ResetStanceTint(Creature creature)
    {
        var sprite = FindSprite(creature);
        if (sprite == null) return;
        if (OriginalTint[creature] is { } original)
            sprite.Modulate = original;
        OriginalTint[creature] = null;
    }

    public static async Task PlayCardAnimation(CardModel card)
    {
        if (card.Type == MegaCrit.Sts2.Core.Entities.Cards.CardType.Attack)
            await TriggerAttack(card);
        else
            await TriggerCast(card);
    }

    private static async Task TriggerAnim(Creature creature, string trigger, string frameAnimation, float delay)
    {
        await CreatureCmd.TriggerAnim(creature, trigger, delay);

        // 兼容当前帧动画场景：游戏触发名与 SpriteFrames 内的小写动画名不同。
        var sprite = FindSprite(creature);
        if (sprite?.SpriteFrames?.HasAnimation(frameAnimation) == true)
            sprite.Play(frameAnimation);
    }

    private static AnimatedSprite2D? FindSprite(Creature creature)
    {
        var visuals = NCombatRoom.Instance?.GetCreatureNode(creature)?.Visuals;
        if (visuals == null) return null;
        if (visuals is not Node node) return null;
        if (node is AnimatedSprite2D direct) return direct;
        return node.FindChildren("*", nameof(AnimatedSprite2D), true, false)
            .OfType<AnimatedSprite2D>()
            .FirstOrDefault();
    }
}
