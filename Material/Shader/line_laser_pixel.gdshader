shader_type canvas_item;

uniform vec4 beam_color : source_color = vec4(0.3, 0.5, 1.0, 1.0);
uniform float brightness : hint_range(1.0, 10.0) = 4.0;

// --- 像素控制 ---
uniform float pixel_size : hint_range(16.0, 128.0) = 64.0; // 虚拟像素精度
uniform int color_levels = 3; // 颜色阶梯数量

// --- 形态控制 ---
uniform float tail_pos : hint_range(-0.1, 1.1) = 0.0;
uniform float head_pos : hint_range(-0.1, 1.1) = 0.0;

void fragment() {
    // 1. 像素化 UV
    vec2 grid_uv = floor(UV * pixel_size) / pixel_size;
    
    // 2. 核心可见性（使用 step 代替 smoothstep，获得硬边缘）
    float head_mask = 1.0 - step(head_pos, grid_uv.x);
    float tail_mask = step(tail_pos, grid_uv.x);
    float beam_mask = head_mask * tail_mask;
    
    // 3. 像素风激光形态（阶梯状发光）
    float dist = abs(grid_uv.y - 0.5) * 2.0;
    // 将 0.0-1.0 的距离切分为有限的层级
    float glow = 1.0 - floor(dist * float(color_levels)) / float(color_levels);
    glow = max(glow, 0.0);
    
    // 4. 像素跳跃动画
    // 使用 floor(TIME) 让能量流动产生“一格一格”跳动的感觉
    float anim_speed = 15.0;
    float wave_x = floor(grid_uv.x * 10.0 - floor(TIME * anim_speed));
    float wave = mod(wave_x, 2.0) * 0.3 + 0.7;
    
    // 5. 最终合成
    float final_alpha = step(0.1, glow) * beam_mask * wave;
    
    vec3 final_color = beam_color.rgb * brightness;
    
    COLOR = vec4(final_color, final_alpha * beam_color.a);
}