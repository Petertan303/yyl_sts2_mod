# yyl_sts2_mod 资源规范

## 音频

- 卡牌、遗物、Power 音效：`res://yyl_sts2_mod/audio/sfx/<分类>/<名称>.ogg`
- 姿态环境音：`res://yyl_sts2_mod/audio/ambience/<名称>.ogg`
- 角色语音：`res://yyl_sts2_mod/audio/voice/<名称>.ogg`
- C# 统一通过 `yylAudio.Sfx(...)`、`yylAudio.Ambience(...)`、`yylAudio.Voice(...)` 生成路径。
- `yylAudio.PlaySfx(...)`、`PlayVoice(...)` 和 `PlayLooped(...)` 会缓存音频、遵循音效音量设置，并对缺失资源只告警一次。

## 帧动画

- 角色场景保持 `AnimatedSprite2D` 作为 `Visuals` 节点。
- 帧动画名称沿用当前角色场景：`attack`、`cast`、`hurt`、`die`、`idle_loop`。
- C# 通过 `yylAnim` 触发攻击、施法、受击和死亡动画，不依赖 Spine 专用节点。
- 姿态切换使用 `yylAnim.PlayStanceTransition(...)` 触发施法动作，并通过 `AnimatedSprite2D.Modulate` 表现姿态色。
- 新姿态默认不需要独立骨骼或 Spine 动画；必要时再叠加独立 VFX 场景。

## VFX

- 姿态与技能场景：`res://yyl_sts2_mod/scenes/vfx/<名称>.tscn`
- VFX 图片：`res://yyl_sts2_mod/images/vfx/<名称>.png`
- 姿态配置集中在各 `StanceVfxConfig` 中，路径缺失时 `StanceVfxController` 会安全跳过。

## 图标

- 卡牌图：`res://yyl_sts2_mod/images/card_portraits/<card_id>.png`
- 大卡图：`res://yyl_sts2_mod/images/card_portraits/big/<card_id>.png`
- Power 图：`res://yyl_sts2_mod/images/powers/<power_id>.png`
- 遗物图：`res://yyl_sts2_mod/images/relics/<relic_id>.png`

文件名使用小写类名；缺少专用图片时会回退到同目录的 `card.png`、`power.png` 或 `relic.png`。
