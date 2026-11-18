shader_type canvas_item;
render_mode blend_add, unshaded;

// 佛光轮 (金光 30 层点亮): 独立于立绘轮廓的解析形状 —— 角色身下最底层的正圆/水滴光轮
// 载体贴图 (golden_sdf.png) 仅提供绘制范围, 形状/条纹/彩虹全部程序化, 不采样 SDF 值
// 形状: 圆的上半部按 drop_taper 收窄成尖 (顶点/底缘/最大宽度都不变), 所以高度恒为 2R;
//       drop_taper = 0 为正圆。R=128 局部单位 × 节点 1.6 倍 → 视觉直径约 410px
//       (角色立绘高 255px 的 1.6 倍), 圆心 -33 ≈ 角色胸部, 光轮整体略靠上。
// ⚠ 节点 scale (2.059, 2.147) = 基准 (1.287, 1.342) 的 1.6 倍 —— 它就是光轮的整体倍率:
//   形状/条纹/渐变全部随节点等比放大, 绘制范围同步变大, 不会被贴图边缘切平。
//   shader 内尺寸参数为局部单位 (1 单位 = 1.6 屏幕 px); 调整体大小改节点 scale,
//   调形状改 shader 参数。⚠ node_scale 是内部标定基准, 勿同步成节点实际 scale (会抵消倍率)。
uniform vec2 tex_size = vec2(385.0, 334.0);    // 载体贴图像素尺寸
uniform vec2 node_scale = vec2(1.287, 1.342);  // 内部单位换算基准 (固定, 不随节点 scale 变)
uniform float intensity = 0.5;

uniform float drop_radius = 128.0;     // 半径 (屏幕 px; 2R = 光轮高度 ≈ 角色高度)
uniform float drop_center_y = -33.0;   // 圆心 y (相对贴图中心, 向下正; -33 ≈ 角色胸部)
uniform float drop_taper = 0.95;       // 上半部收窄量 (0 = 正圆, 越大越像水滴)
uniform float edge_soft = 10.0;        // 轮廓柔边宽 (屏幕 px)

uniform float stripes = 10.0;          // 放射条纹数
uniform float stripe_sharp = 2.2;      // 条纹锐度 (pow 指数)
uniform float stripe_bend = 0.14;      // 条纹随半径的弯曲量
uniform float spin_speed = 0.06;       // 整体旋转 rad/s
uniform float hue_speed = 0.02;        // 彩虹流动 圈/s
uniform float hue_sweep = 0.30;        // 半径方向色相偏移幅度
uniform float sat = 0.75;              // 彩虹饱和度

// ====== 色彩分层配置 ======
uniform vec3 core_color = vec3(1.0, 0.96, 0.78); // 中心发光白金
uniform float core_mix = 0.9;                    // 白金核心混合强度
uniform vec3 gold_color = vec3(1.0, 0.82, 0.35); // 光轮主体金色 (温暖纯正金黄)
uniform float rim_start = 0.55;                  // 边缘彩虹起始位置 (0.0~1.0): 0.6→0.55 (11-17 用户定调, 彩虹往中心挪一点)
uniform float rim_power = 1.2;                   // 边缘彩虹过渡曲线 (越大过渡越陡、彩虹越聚拢在边缘)
// ★2026-11-17: 全彩虹模式 —— 金光≥20层且处于祖炁形态时, 由 GoldenAegis 置 1,
//   让整个光轮从"金身+边缘彩虹"变为通体彩虹 (呼应初版彩虹外观, 与祖炁联动)。
uniform float rainbow_full = 0.0;
uniform float rainbow_full_sat = 0.90;           // 全彩虹模式饱和度 (平时 sat=0.75)
uniform float rainbow_core_keep = 0.70;          // 全彩虹时中心保留的金核强度: 1-keep 是中心金占比 (0=纯彩虹; 越大金核越亮)

vec3 hue2rgb(float h) {
    vec3 p = abs(fract(h + vec3(0.0, 2.0 / 3.0, 1.0 / 3.0)) * 6.0 - 3.0);
    return clamp(p - 1.0, 0.0, 1.0);
}

void fragment() {
    // 贴图局部坐标 (屏幕 px, 原点 = 贴图中心 = 立绘中心, y 向下)
    vec2 p = (UV - 0.5) * tex_size * node_scale;
    vec2 cpx = p - vec2(0.0, drop_center_y);

    // 水滴/正圆 SDF (近似): 上半部 x 方向收窄成尖, 顶点/底缘/最大宽度保持不变
    float up = clamp(-cpx.y / drop_radius, 0.0, 1.0);
    float width = max(drop_radius * (1.0 - drop_taper * pow(up, 1.2)), 1.0);
    vec2 e = vec2(cpx.x / width, cpx.y / drop_radius);
    float sd = (length(e) - 1.0) * drop_radius;
    float body = 1.0 - smoothstep(-edge_soft, edge_soft, sd);

    // 极坐标 (原点 = 圆心 = 角色胸部, rad 归一化到 drop_radius)
    vec2 c = cpx / drop_radius;
    float rad = length(c);
    float ang = atan(c.y, c.x);
    float t = TIME;

    // 放射条纹: 主层 + 反向次层, 角度随半径轻微弯曲破除完美周期
    float a2 = ang + t * spin_speed + stripe_bend * sin(rad * 9.0 + t * 0.3);
    float s1 = pow(0.5 + 0.5 * cos(a2 * stripes), stripe_sharp);
    float s2 = pow(0.5 + 0.5 * cos(-a2 * stripes * 2.0 + 1.7), stripe_sharp * 1.5) * 0.45;
    float s_env = smoothstep(0.02, 0.14, rad); // 圆心留白, 条纹自内圈展开

    // 彩虹: 色相 = 角度 + 时间流动 + 半径渐变; 降饱和至 sat (采用感知亮度加权去色)
    float hue = fract(ang / 6.28318530 + t * hue_speed + rad * hue_sweep);
    vec3 rgb_color = hue2rgb(hue);
    float luma = dot(rgb_color, vec3(0.2126, 0.7152, 0.0722));
    // ★全彩虹模式用更高饱和度 sat; 平时用 sat (0.75)。
    vec3 rainbow = mix(vec3(luma), rgb_color, mix(sat, rainbow_full_sat, rainbow_full));

    // ====== 颜色三段式渐变 ======
    // 1. 中心核心白金 → 中段主体金色
    float core = exp(-rad * 4.5);
    vec3 base_gold = mix(gold_color, core_color, core * core_mix);

    // 2. 主体金色 → 边缘彩虹
    // length(e) 在水滴外轮廓处恒等于 1.0，由此计算出的边缘遮罩能严丝合缝贴合水滴外圈
    float shape_edge = length(e);
    float rim = smoothstep(rim_start, 1.0, shape_edge);
    rim = pow(rim, rim_power);
    // ★全彩虹模式: 11-17 用户定调 —— 中心保留一点金核, 不再是纯通体彩虹。
    //   core 在中心≈1、向外快速衰减; fullTarget = 1 - core*keep: 中心留 keep 比例的金,
    //   外围 core≈0 → fullTarget≈1 仍是全彩虹。keep=0.30 → 中心约 30% 金 + 70% 彩虹。
    float fullTarget = 1.0 - core * rainbow_core_keep;
    rim = mix(rim, fullTarget, rainbow_full);
    vec3 col = mix(base_gold, rainbow, rim);

    // 动态呼吸与法向衰减
    float pulse = 0.85 + 0.15 * sin(t * 1.7);
    float lum = (0.30 + 0.85 * (s1 + s2) * s_env) * pulse;
    float fade = smoothstep(0.0, drop_radius * 0.55, -sd);

    COLOR = vec4(col * lum, body * fade * intensity);
}