shader_type canvas_item;

group_uniforms Global_Settings;
uniform float effect_opacity : hint_range(0.0, 1.0) = 1.0; 
uniform float global_alpha : hint_range(0.0, 1.0) = 1.0;  

// --- 新增：中心渐变设置 ---
group_uniforms Center_Gradient;
uniform float gradient_radius : hint_range(0.0, 1.0) = 0.5; // 渐变核心半径
uniform float gradient_softness : hint_range(0.0, 1.0) = 0.5; // 边缘羽化程度
uniform bool apply_to_background = false; // 是否让背景也随渐变消失

group_uniforms Stars;
uniform float star_density : hint_range(1.0, 100.0) = 25.0; 
uniform float star_size : hint_range(0.001, 0.1) = 0.05;
uniform vec3 star_color : source_color = vec3(1.0);

group_uniforms Meteor;
uniform vec3 meteor_color : source_color = vec3(0.8, 0.9, 1.0);
uniform float meteor_speed : hint_range(0.1, 5.0) = 2.0;
uniform float meteor_probability : hint_range(0.0, 1.0) = 0.2; 

highp float hash21(highp vec2 p) {
    p = fract(p * vec2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return fract(p.x * p.y);
}

highp vec2 hash22(highp vec2 p) {
    vec3 p3 = fract(vec3(p.xyx) * vec3(0.1031, 0.1030, 0.0973));
    p3 += dot(p3, p3.yzx + 33.33);
    return fract((p3.xx + p3.yz) * p3.zy);
}

void fragment() {
    highp vec2 uv = UV;
    
    // --- 1. 计算中心距离渐变 ---
    // dist_from_center 在中心为 0，边缘增加
    float dist_from_center = distance(uv, vec2(0.5));
    // mask 在中心为 1，向边缘衰减为 0
    float mask = smoothstep(gradient_radius + gradient_softness, gradient_radius, dist_from_center);

    // 2. 获取基础纹理
    vec4 base_tex = texture(TEXTURE, uv);
    vec3 effect_rgb = vec3(0.0);

    // --- 3. 星星层 ---
    highp vec2 star_uv = uv * star_density; 
    highp vec2 grid_pos = floor(star_uv);
    highp vec2 grid_uv = fract(star_uv) - 0.5;
    float r = hash21(grid_pos); 
    vec2 offset = (hash22(grid_pos) - 0.5) * 0.8;
    float dist = length(grid_uv - offset);
    float star = smoothstep(star_size, 0.0, dist);
    float twinkle = sin(TIME * 2.5 + r * 20.0) * 0.5 + 0.5;
    effect_rgb += star * step(0.85, r) * twinkle * star_color;

    // --- 4. 流星层 ---
    highp vec2 m_grid_uv = uv * 2.0; 
    highp vec2 m_id = floor(m_grid_uv);
    float m_h = hash21(m_id);
    if (m_h < meteor_probability) {
        float rand_angle = m_h * 6.2831; 
        mat2 m_rot = mat2(vec2(cos(rand_angle), -sin(rand_angle)), vec2(sin(rand_angle), cos(rand_angle)));
        vec2 local_m_uv = (fract(m_grid_uv) - 0.5) * m_rot;
        float period = 3.0 + m_h * 5.0;
        float phase = mod(TIME * meteor_speed + m_h * 20.0, period);
        float progress = phase / 0.8; 
        if (progress < 1.0) {
            float x = local_m_uv.x - (progress - 0.5) * 2.0; 
            float y = local_m_uv.y;
            float head = smoothstep(0.015, 0.0, length(vec2(x, y)));
            float tail = smoothstep(0.0, -0.4, x) * smoothstep(0.01, 0.0, abs(y));
            tail *= exp(x * 10.0);
            effect_rgb += meteor_color * (head + tail) * (1.0 - progress);
        }
    }
    
    // --- 5. 应用效果透明度与中心渐变遮罩 ---
    effect_rgb *= effect_opacity;
    effect_rgb *= mask; // 星星和流星受渐变控制
    
    // --- 6. 混合与最终输出 ---
    vec3 final_rgb = base_tex.rgb + effect_rgb;
    
    // 计算最终 Alpha
    float final_alpha = base_tex.a * global_alpha;
    
    // 如果开启，让背景也随中心渐变消失
    if (apply_to_background) {
        final_alpha *= mask;
    }

    COLOR = vec4(final_rgb, final_alpha);
}