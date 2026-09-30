shader_type canvas_item;
render_mode unshaded, blend_add;

uniform vec4 effect_color : source_color = vec4(0.28, 0.78, 1.0, 1.0);
uniform float pulse_progress : hint_range(0.0, 1.0) = 0.0;
uniform float segment_count : hint_range(1.0, 16.0) = 6.0;
uniform float swirl_strength : hint_range(0.0, 1.0) = 0.0;

void fragment()
{
    vec2 centered_uv = UV * 2.0 - vec2(1.0);
    float distance_from_center = length(centered_uv);
    float angle = atan(centered_uv.y, centered_uv.x);
    float expanding_radius = mix(0.12, 0.96, pulse_progress);
    float ring_width = mix(0.075, 0.018, pulse_progress);
    float outer_ring = 1.0 - smoothstep(ring_width, ring_width * 2.0, abs(distance_from_center - expanding_radius));
    float inner_ring = 1.0 - smoothstep(0.018, 0.055, abs(distance_from_center - expanding_radius * 0.58));
    float segments = 0.55 + 0.45 * sin(angle * segment_count + TIME * (2.0 + swirl_strength * 4.0) + distance_from_center * swirl_strength * 18.0);
    float fill = (1.0 - smoothstep(0.0, expanding_radius, distance_from_center)) * 0.12;
    float fade = (1.0 - pulse_progress) * (1.0 - smoothstep(0.98, 1.0, distance_from_center));
    float alpha = (outer_ring * (0.65 + segments * 0.35) + inner_ring * 0.22 + fill) * fade;

    COLOR = vec4(effect_color.rgb * (1.0 + outer_ring * 0.7), effect_color.a * alpha);
}
