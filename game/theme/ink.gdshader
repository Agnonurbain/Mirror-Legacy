// Ink spreading over the page (Shuimo transition): from the centre outwards, its edge ragged like ink in wet paper.
shader_type canvas_item;

uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform vec4 ink : source_color = vec4(0.13, 0.12, 0.11, 1.0);

float hash(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }

float noise(vec2 p)
{
    vec2 i = floor(p);
    vec2 f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    return mix(mix(hash(i), hash(i + vec2(1.0, 0.0)), f.x), mix(hash(i + vec2(0.0, 1.0)), hash(i + vec2(1.0, 1.0)), f.x), f.y);
}

void fragment()
{
    vec2 d = (UV - 0.5) * vec2(1.78, 1.0);
    float bleed = noise(UV * 9.0) * 0.35 + noise(UV * 31.0) * 0.12;
    float reach = progress * 1.35;
    float covered = smoothstep(reach, reach - 0.08, length(d) + bleed * 0.6);
    COLOR = vec4(ink.rgb, covered * ink.a);
}
