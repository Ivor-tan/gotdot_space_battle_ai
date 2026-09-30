shader_type canvas_item;

// ===== 浮动参数 =====
uniform float amplitude = 10.0;
uniform float speed = 2.0;
uniform float phase = 0.0;

// 控制XY方向权重
uniform vec2 motion_scale = vec2(1.0, 1.0);

// ===== 呼吸参数 =====
uniform bool enable_breath = true;
uniform float breath_speed = 1.5;
uniform float breath_strength = 0.3;

// ===== 发光参数 =====
uniform vec4 glow_color : source_color = vec4(1.0, 0.8, 0.3, 1.0);
uniform float glow_size = 2.0;
uniform float glow_intensity = 1.0;

void vertex() {
    float t = TIME * speed + phase;

    // 基础浮动（XY）
    float offset_x = cos(t * 0.8) * amplitude * motion_scale.x;
    float offset_y = sin(t) * amplitude * motion_scale.y;

    // 呼吸影响位移（可选）
    if (enable_breath) {
        float breath = 1.0 + sin(TIME * breath_speed) * breath_strength;
        offset_x *= breath;
        offset_y *= breath;
    }

    VERTEX.xy += vec2(offset_x, offset_y);
}

void fragment() {
    vec2 pixel = TEXTURE_PIXEL_SIZE * glow_size;

    vec4 base = texture(TEXTURE, UV);

    // ===== 软发光（多层采样）=====
    float alpha = 0.0;

    // 第一圈（权重大）
    alpha += texture(TEXTURE, UV + vec2(pixel.x, 0.0)).a;
    alpha += texture(TEXTURE, UV + vec2(-pixel.x, 0.0)).a;
    alpha += texture(TEXTURE, UV + vec2(0.0, pixel.y)).a;
    alpha += texture(TEXTURE, UV + vec2(0.0, -pixel.y)).a;

    // 对角
    alpha += texture(TEXTURE, UV + vec2(pixel.x, pixel.y)).a;
    alpha += texture(TEXTURE, UV + vec2(-pixel.x, pixel.y)).a;
    alpha += texture(TEXTURE, UV + vec2(pixel.x, -pixel.y)).a;
    alpha += texture(TEXTURE, UV + vec2(-pixel.x, -pixel.y)).a;

    alpha /= 8.0;

    // 第二圈（更远，更弱）→ 更柔和
    float alpha2 = 0.0;
    vec2 pixel2 = pixel * 2.0;

    alpha2 += texture(TEXTURE, UV + vec2(pixel2.x, 0.0)).a;
    alpha2 += texture(TEXTURE, UV + vec2(-pixel2.x, 0.0)).a;
    alpha2 += texture(TEXTURE, UV + vec2(0.0, pixel2.y)).a;
    alpha2 += texture(TEXTURE, UV + vec2(0.0, -pixel2.y)).a;

    alpha2 /= 4.0;

    float glow = max(alpha - base.a, 0.0);
    glow += alpha2 * 0.5;

    // ===== 呼吸影响发光 =====
    float glow_pulse = 1.0;
    if (enable_breath) {
        glow_pulse = 0.7 + 0.3 * sin(TIME * breath_speed);
    }

    vec4 glow_result = glow_color * glow * glow_intensity * glow_pulse;

    // ===== 合成 =====
    COLOR = base + glow_result;
}