shader_type canvas_item;
render_mode blend_mix;

uniform float speed = 0.2;

void fragment()
{

   vec2 uv = UV - 0.5;
   float t = TIME * speed;
   mat2 rot = mat2(
        vec2(cos(t), -sin(t)),
        vec2(sin(t), cos(t))
    );

   uv = rot * uv;
   uv += 0.5;

   if (uv.x < 0.0 || uv.x > 1.0|| uv.y < 0.0 || uv.y > 1.0)
        discard;

    COLOR = texture(TEXTURE, uv);
}