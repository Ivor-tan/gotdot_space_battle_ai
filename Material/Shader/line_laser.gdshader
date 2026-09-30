shader_type canvas_item;

// --- 科幻颜色与亮度 ---
uniform vec4 beam_color : source_color = vec4(0.3, 0.5, 1.0, 1.0);
uniform float brightness : hint_range(1.0, 10.0) = 4.0;

// --- 形态控制 (关键) ---
// 均在 0.0 到 1.0 之间。UV.x 小于 tail 或 大于 head 的区域透明。
uniform float tail_pos : hint_range(-0.1, 1.1) = 0.0; // 尾部(起点)
uniform float head_pos : hint_range(-0.1, 1.1) = 0.0; // 头部(终点)

// --- 动画细节 ---
uniform float edge_softness : hint_range(0.01, 0.3) = 0.05; // 激光两头的柔和度

void fragment() {
    vec2 uv = UV;
    
    // 1. 核心可见性遮罩 (关键逻辑)
    // 利用两个 smoothstep 创建一个随位置滑动的“可见窗口”
    // 头部截断 (终点)
    float head_mask = 1.0 - smoothstep(head_pos - edge_softness, head_pos, uv.x);
    // 尾部截断 (起点)
    float tail_mask = smoothstep(tail_pos - edge_softness, tail_pos, uv.x);
    
    // 只有在 tail_pos 和 head_pos 之间的区域才是可见的
    float beam_mask = head_mask * tail_mask;
    
    // 2. 基础激光形态 (中间亮两边淡)
    float dist_from_center = abs(uv.y - 0.5) * 2.0;
    float glow = exp(-pow(dist_from_center * 3.0, 2.0));
    
    // 3. 能量流动效果 (可选)
    float wave = sin(uv.x * 20.0 - TIME * 30.0) * 0.2 + 0.8;
    
    // 4. 合并所有遮罩
    float final_alpha = glow * beam_mask * wave;
    
    COLOR = vec4(beam_color.rgb * brightness, final_alpha * beam_color.a);
}