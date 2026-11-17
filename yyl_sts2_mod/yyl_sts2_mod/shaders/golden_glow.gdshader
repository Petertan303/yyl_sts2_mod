shader_type canvas_item;
render_mode unshaded;

// 光边缘中心: 0.5 = 贴轮廓, 0.5 + n/144 = 外扩 n px, 0.5 - n/144 = 内收 n px
uniform float threshold = 0.5;
// 柔边宽度 (归一化): 0.007 ≈ 1px 硬边, 0.125 ≈ 18px 光晕
uniform float soft = 0.01;
uniform vec4 glow_color : source_color = vec4(0.92, 0.85, 0.39, 1.0);
uniform vec4 glow_color_2 : source_color = vec4(1.0, 0.82, 0.35, 1.0);
// 明度 (RefreshVfx 按层数设置)
uniform float intensity = 1.0;

void fragment() {
    float d = texture(TEXTURE, UV).r;
    float a = 1.0 - smoothstep(threshold - soft, threshold + soft, d);
    COLOR = vec4(glow_color_2.rgb, glow_color_2.a * a * intensity);
}
