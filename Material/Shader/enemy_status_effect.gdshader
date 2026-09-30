shader_type canvas_item;
render_mode unshaded;

uniform float burn_strength : hint_range(0.0, 1.0) = 0.0;
uniform float slow_strength : hint_range(0.0, 1.0) = 0.0;
uniform float shield_active : hint_range(0.0, 1.0) = 0.0;

void fragment()
{
    vec4 base_color = texture(TEXTURE, UV);
    // Enemy status colors remain constant throughout the effect.
    vec3 burn_color = vec3(0.52, 0.11, 0.02);
    vec3 slow_color = vec3(0.05, 0.32, 0.42);
    vec3 status_color = burn_color * burn_strength + slow_color * slow_strength;
    float status_strength = max(burn_strength, slow_strength);
    base_color.rgb += status_color * base_color.a;
    base_color.rgb = mix(base_color.rgb, base_color.rgb * (1.0 + status_strength * 0.22), status_strength);
    vec2 pixel = TEXTURE_PIXEL_SIZE;
    float nearby_alpha = 0.0;
    nearby_alpha = max(nearby_alpha, texture(TEXTURE, UV + vec2(pixel.x, 0.0)).a);
    nearby_alpha = max(nearby_alpha, texture(TEXTURE, UV + vec2(-pixel.x, 0.0)).a);
    nearby_alpha = max(nearby_alpha, texture(TEXTURE, UV + vec2(0.0, pixel.y)).a);
    nearby_alpha = max(nearby_alpha, texture(TEXTURE, UV + vec2(0.0, -pixel.y)).a);
    float outline = max(nearby_alpha - base_color.a, 0.0) * shield_active;
    COLOR = vec4(mix(base_color.rgb, vec3(1.0), outline), max(base_color.a, outline));
}
