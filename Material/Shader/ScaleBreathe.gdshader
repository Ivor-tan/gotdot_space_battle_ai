shader_type canvas_item;

// 定义可调节参数
uniform float breathe_speed : hint_range(0.1, 10.0) = 2.0; // 呼吸速度
uniform float breathe_amount : hint_range(0.01, 0.5) = 0.1; // 呼吸幅度 (0.1 表示放大 10%)

void vertex() {
    // 核心逻辑：利用 sin 函数生成周期性数值
    // sin(TIME * speed) -> 在 -1 到 1 之间波动
    // * amount -> 调整缩放范围
    float scale = 1.0 + sin(TIME * breathe_speed) * breathe_amount;
    
    // 应用到顶点坐标
    // 注意：默认是绕 CanvasItem 的 (0,0) 点缩放，
    // 如果需要绕图片中心缩放，请确保 Sprite2D 的 Centered 属性已勾选。
    VERTEX *= scale;
}

void fragment() {
    // 这里保持原样，只改变顶点
}