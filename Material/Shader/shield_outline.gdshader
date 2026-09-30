shader_type canvas_item;
render_mode unshaded;

uniform float shield_active : hint_range(0.0, 1.0) = 0.0;

void fragment()
{
    vec4 base_color = texture(TEXTURE, UV);
    vec2 pixel = TEXTURE_PIXEL_SIZE;
    float nearby_alpha = 0.0;
    nearby_alpha = max(nearby_alpha, texture(TEXTURE, UV + vec2(pixel.x, 0.0)).a);
    nearby_alpha = max(nearby_alpha, texture(TEXTURE, UV + vec2(-pixel.x, 0.0)).a);
    nearby_alpha = max(nearby_alpha, texture(TEXTURE, UV + vec2(0.0, pixel.y)).a);
    nearby_alpha = max(nearby_alpha, texture(TEXTURE, UV + vec2(0.0, -pixel.y)).a);
    nearby_alpha = max(nearby_alpha, texture(TEXTURE, UV + vec2(pixel.x, pixel.y)).a);
    nearby_alpha = max(nearby_alpha, texture(TEXTURE, UV + vec2(-pixel.x, pixel.y)).a);
    nearby_alpha = max(nearby_alpha, texture(TEXTURE, UV + vec2(pixel.x, -pixel.y)).a);
    nearby_alpha = max(nearby_alpha, texture(TEXTURE, UV + vec2(-pixel.x, -pixel.y)).a);
    float outline = max(nearby_alpha - base_color.a, 0.0) * shield_active;
    vec3 outlined = mix(base_color.rgb, vec3(1.0), outline);
    COLOR = vec4(outlined, max(base_color.a, outline));
}
