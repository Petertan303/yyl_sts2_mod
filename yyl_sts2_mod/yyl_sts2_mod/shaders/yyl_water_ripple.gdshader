shader_type canvas_item;
render_mode unshaded;

// 水波纹空间扭曲 (自研, 不依赖原版资源):
// 径向正弦波纹推挤屏幕画面 + RGB 色差分离, 圆形范围内生效、边缘平滑淡出。
// 强度/波纹相位由 C# (yylVfx.WaterRipple) 的 Tween 驱动: 强度 0→峰值→0, 相位 0→1 向外扩散。
// ⚠ 必须有 BackBufferCopy 在本节点之前拷屏 (WaterRipple 组装时已加), 否则读到空屏幕 = 无效果。

uniform sampler2D screen_tex : hint_screen_texture, filter_linear, repeat_disable;
uniform float intensity : hint_range(0.0, 0.2) = 0.05; // 峰值扭曲强度 (屏幕 UV 单位)
uniform float chroma = 0.45;                           // 色差相对强度 (× 局部偏移量)
uniform float rings = 3.0;                             // 波纹圈数
uniform float phase = 0.0;                             // 波纹扩散相位 0..1 (C# 驱动)

void fragment() {
    float mask = texture(TEXTURE, UV).a; // 径向遮罩: 中心 1, 边缘 0
    vec2 c = UV - 0.5;
    float r = length(c) * 2.0;

    // 环形正弦波, 相位随扩散推进 -> 波纹向外跑
    float wave = sin((r * rings - phase * rings) * 6.2831853);
    vec2 dir = length(c) > 1e-4 ? normalize(c) : vec2(0.0);

    // 扭曲量: 波纹 × 强度 × 遮罩 × 外圈衰减
    float amp = intensity * mask * (1.0 - smoothstep(0.65, 1.0, r));
    vec2 off = dir * wave * amp;

    // 屏幕重采样 + RGB 分离 (色差让折射感更强)
    vec2 suv = SCREEN_UV + off;
    vec2 ca = off * chroma;
    vec3 col;
    col.r = texture(screen_tex, suv + ca).r;
    col.g = texture(screen_tex, suv).g;
    col.b = texture(screen_tex, suv - ca).b;

    COLOR = vec4(col, mask);
}
