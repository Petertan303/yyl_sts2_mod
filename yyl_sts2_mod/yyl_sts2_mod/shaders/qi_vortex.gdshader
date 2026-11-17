shader_type canvas_item;
render_mode blend_add, unshaded;

// 炁 / 祖炁形态的程序化特效 (自研, 不依赖原版资源):
//   mode = 0 -> 丹田气旋: 角色脚下的透视压扁同心环 + 环上流动亮点, 青碧色;
//               层数驱动亮度/环数/转速, 8 炁拐点由青碧渐变为金色 (warm)。
//   mode = 1 -> 祖炁光柱: 贯穿身体的竖向光带 + 外环, 蓝色 (2026-11-17 改, 与金光护体的金色区分);
//               升级后追加第二圈外环 (form_level)。光带比初版更宽, 上下柔化区更大以消除硬截断。
//   两个 Sprite2D 各持一个本 shader 的 ShaderMaterial, 由 QiVfx.Refresh 驱动参数。
//   形态激活时对两者 reveal 做反向 tween: 气旋淡出 -> 光柱淡入 ("气旋提成光柱")。
//   载体贴图用 yyl_water_ripple_mask.png 仅提供 UV 矩形 (256x256), 形状全部程序化。

uniform float mode = 0.0;        // 0 = 丹田气旋, 1 = 祖炁光柱
uniform float intensity = 0.5;   // 层数驱动的亮度 (C# 封顶)
uniform float reveal = 1.0;      // 显隐过渡 (形态切换时 tween)
uniform float stacks = 0.0;      // 当前炁层数
uniform float form_level = 0.0;  // 0 = 未升级(9%), 1 = 升级(12.5%)
uniform float warm = 0.0;        // 8 炁拐点换色 0=青碧, 1=金

uniform vec3 qi_color = vec3(0.18, 0.82, 0.70);     // 青碧
uniform vec3 gold_color = vec3(1.00, 0.82, 0.35);   // 金 (气旋暖化用)
uniform vec3 pillar_color = vec3(0.22, 0.55, 1.00); // 祖炁光柱: 蓝 (2026-11-17, 与金光区分)

void fragment() {
    vec2 p = UV - 0.5;
    float t = TIME;
    vec3 col = qi_color;
    float a = 0.0;

    if (mode < 0.5) {
        // ── 丹田气旋 ── 节点 scale 已把 y 压扁成椭圆, shader 内按正圆画
        float r = length(p) * 2.0;             // 0..1 (边缘)
        float ang = atan(p.y, p.x);
        float spin = t * (0.28 + min(stacks, 33.0) * 0.008);
        float flow = ang + spin;

        float ring1 = exp(-pow((r - 0.42) * 16.0, 2.0));
        float ring2 = exp(-pow((r - 0.68) * 18.0, 2.0)) * 0.70;
        // 第三圈随层数 (>8) 渐显, 高炁封顶
        float outer_gate = clamp((stacks - 8.0) / 12.0, 0.0, 1.0);
        float ring3 = exp(-pow((r - 0.90) * 20.0, 2.0)) * outer_gate * 0.55;

        // 角向流光条纹: 转速随炁略增, 密度固定, 避免相位跳变
        float stripes = pow(0.5 + 0.5 * cos(flow * 8.0), 3.0);
        a = (ring1 + ring2 + ring3) * (0.42 + 0.58 * stripes);

        // 上升亮点: 沿环旋转的火花 + 中心微光
        float spark = pow(max(0.0, cos(flow * 14.0)), 24.0);
        spark *= exp(-pow((r - 0.58) * 12.0, 2.0));
        a += spark * 0.85;
        a += exp(-r * r * 30.0) * 0.22;

        col = mix(qi_color, gold_color, warm);
    } else {
        // ── 祖炁倒锥 ── 脚底窄、向上张开的倒锥形光雾, 底金顶蓝 (2026-11-17 重做)
        //   h: 0 = 脚底(贴图下缘 UV.y=1), 1 = 上方(贴图上缘 UV.y=0)。
        //   canvas_item 的 UV.y=0 在贴图上缘, 故 h = 1 - UV.y。
        //   ★与金光"一体": 底部用金色衔接金光护体的描边, 越往上越蓝以区分层次。
        // ★2026-11-17 修订: 锥体有效高度 hRaw∈[0.10,0.75] —— 底部抬到脚底圆盘处
        //   (hRaw=0 是节点最底、落在血条附近; 0.10 对应全局 y≈-12 ≈ 气旋圆盘位置),
        //   顶部到角色 3/4 高, 两端之外都不画。
        float hRaw = 1.0 - UV.y;
        float h = clamp((hRaw - 0.10) / 0.56, 0.0, 1.0);  // 0=脚底圆盘, 1=角色3/4高处

        // ★2026-11-17 用户定调: 底部内收、上半外扩。
        //   halfW 底 0.30 (锥底收窄) → 顶 0.62 (超出画布半宽 0.5, 上部充分张开, 靠下面的
        //   xfade 在边缘淡出, 不会出现硬切)。相比旧值 0.40→0.50, 下更收/上更扩。
        float halfW = mix(0.30, 0.62, h);
        float dx = abs(p.x) / max(halfW, 0.001);   // 0 = 锥轴, 1 = 锥边

        // 锥体主体 (横向软高斯) + 中心亮芯
        float body = exp(-dx * dx * 2.0);
        float core = exp(-dx * dx * 6.0) * 0.50;

        // 锥侧轮廓光 (淡, 不喧宾夺主)
        float edge = exp(-pow((dx - 0.92) * 2.0, 2.0)) * 0.26;
        float edge2 = exp(-pow((dx - 1.12) * 2.4, 2.0)) * form_level * 0.22;

        // ★★真正的边缘柔化 + 内收外扩: 横向包络让光雾到画布边缘渐隐归零(消硬边)。
        //   ★2026-11-17 再修: 淡出起点必须随 h 变化 —— 固定 0.72 会把 halfW 的
        //   底窄顶宽变化抹平(上下都被削到同一宽度 → 直上直下)。
        //   现在: 底部(x=0)从 p.x*2=0.46 开始淡(收窄), 顶部(x=1)从 0.92 才淡(外扩)。
        float fadeStart = mix(0.30, 0.92, h);
        float xfade = 1.0 - smoothstep(fadeStart, 1.0, abs(p.x) * 2.0);

        // 上下淡入淡出 + 有效高度裁剪
        float bottomFade = smoothstep(0.0, 0.15, h);
        float topFade = 1.0 - smoothstep(0.70, 1.0, h);
        float inHeight = step(0.10, hRaw) * (1.0 - step(0.75, hRaw));
        float mask = xfade * bottomFade * topFade * inHeight;
        a = (body + core + edge + edge2) * mask;

        // 沿锥上升的流动纹理 (底数 0.70→0.80, 平均更亮、起伏更柔)
        a *= 0.80 + 0.20 * sin(h * 15.0 - t * 2.6);

        // 底金顶蓝: 与金光同色起步, 平滑过渡到蓝。
        col = mix(gold_color, pillar_color, smoothstep(0.0, 0.65, h));
    }

    a *= intensity * reveal;
    COLOR = vec4(col, clamp(a, 0.0, 1.0));
}
