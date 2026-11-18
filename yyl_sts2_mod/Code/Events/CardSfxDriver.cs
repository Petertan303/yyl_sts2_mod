using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Cards.Ancient;
using yyl_sts2_mod.Code.Cards.Basic;
using yyl_sts2_mod.Code.Cards.Common;
using yyl_sts2_mod.Code.Cards.Rare;
using yyl_sts2_mod.Code.Cards.Token;
using yyl_sts2_mod.Code.Cards.Uncommon;
using yyl_sts2_mod.Code.Utils;

namespace yyl_sts2_mod.Code.Events;

/// <summary>
///     主题出牌音效驱动 (隐藏 Power): 玩家打出<list type="bullet">
///     <item>清单里<strong>有明确意象</strong>的卡时, 播一段对应的短音效点题;</item>
///     </list>
///     纯数值牌 (打击/防御/寸劲等) 不注册, 所以不会像旧版"每张牌都响"那样吵。
///     <para>
///         音效来自 Kenney CC0 公有领域包 (Impact / RPG Audio / Sci-Fi Sounds),
///         可商用、无需署名。文件在 <c>audio/sfx/cards/</c>, 全部 0.1–2 秒短音。
///     </para>
///     <para>
///         ★ 只响应 <c>Owner.Player</c> 自己打出的牌 —— 联机时不替队友播音效。
///         按 <see cref="CardModel.GetType" /> 查字典 (照 <see cref="GlowRegistry" /> 范式),
///         一张卡只对应一个音; 连打多的牌 (连雷/阿威十八式) 特意选 0.24–0.54s 的短音避免叠加。
///         全程无异常风险: <c>yylAudio</c> 内部已做资源缺失/音量设置保护。
///     </para>
/// </summary>
public sealed class CardSfxDriver : yylPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>对玩家完全隐藏 (不占 buff 槽位、无图标)。</summary>
    protected override bool IsVisibleInternal => false;

    /// <summary>整体音量。比旧版 0.5 略低, 因为现在音更密但更短。</summary>
    private const float Volume = 0.55f;

    /// <summary>相对 <c>audio/sfx/</c> 的路径 (经 yylAudio.Sfx 解析)。</summary>
    private static readonly Dictionary<Type, string> CardSfx = new()
    {
        // ---- 雷系 ----
        [typeof(PalmThunder)] = "cards/scifi/laserSmall_000.ogg",     // 掌心雷 · 短脆电击 (0.24s, 初始牌)
        [typeof(ChainThunder)] = "cards/scifi/laserRetro_000.ogg",    // 连雷 · 短电 (0.24s, 会连打)
        [typeof(YinLei)] = "cards/scifi/laserSmall_002.ogg",           // 引雷 · 短电 (0.25s)
        [typeof(FiveThunderLaw)] = "cards/scifi/laserLarge_000.ogg",   // 五雷正法 · 大电击 (0.68s, 稀有)
        [typeof(BloodThunder)] = "cards/scifi/lowFrequency_explosion_001.ogg", // 血雷 · 低频轰 (1.0s)

        // ---- 剑法 ----
        [typeof(WardingBlade)] = "cards/rpg/knifeSlice.ogg",           // 辟邪剑法 · 剑气挥砍 (0.60s, 先古)

        // ---- 拳法 ----
        [typeof(BurstFist)] = "cards/impact/impactPunch_heavy_000.ogg",    // 崩拳 · 重拳 (0.65s)
        [typeof(ShoulderStrike)] = "cards/impact/impactWood_heavy_000.ogg", // 铁山靠 · 木桩闷响 (0.31s)
        [typeof(ElbowStrike)] = "cards/impact/impactPunch_medium_000.ogg",  // 顶肘 · 中拳 (0.43s)
        [typeof(EighteenForms)] = "cards/impact/impactPunch_medium_002.ogg",// 阿威十八式 · 中拳 (0.54s, 多段)

        // ---- 蛇 ----
        [typeof(WhiteWorm)] = "cards/impact/impactGlass_heavy_001.ogg",    // 白长虫 · 高频裂 (0.43s, 先古)
        [typeof(SnakeFlowerBite)] = "cards/scifi/slime_000.ogg",            // 蛇花咬 · 黏滑 (0.50s)

        // ---- 火/爆 ----
        [typeof(SkyFire)] = "cards/scifi/explosionCrunch_001.ogg",         // 天火 · 大爆炸 (1.36s, 稀有)

        // ---- 禅意/桩功 ----
        [typeof(TaiChi)] = "cards/impact/impactBell_heavy_002.ogg",        // 太极 · 禅钟 (0.70s)
        [typeof(PeasantDrill)] = "cards/rpg/cloth2.ogg",                   // 老农功 · 衣袂 (0.42s)
        [typeof(QiBreath)] = "cards/rpg/cloth4.ogg",                       // 吐纳 · 衣袂 (0.38s)
        [typeof(HorseStance)] = "cards/impact/impactBell_heavy_003.ogg",   // 马步 · 禅钟 (0.65s)

        // ---- 金光 ----
        [typeof(GoldenCharm)] = "cards/impact/impactPlate_medium_000.ogg", // 金光咒 · 金属板 (0.61s, 初始牌)
        [typeof(GoldenWall)] = "cards/impact/impactMetal_light_000.ogg",   // 金光壁 · 轻金属 (0.35s)
    };

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var card = cardPlay?.Card;
        if (card == null) return Task.CompletedTask;
        // 只认自己打出的牌 (联机隔离)
        if (Owner.Player == null || card.Owner != Owner.Player) return Task.CompletedTask;

        if (!CardSfx.TryGetValue(card.GetType(), out var sfx)) return Task.CompletedTask;
        yylAudio.PlaySfx(sfx, Volume);
        return Task.CompletedTask;
    }
}
