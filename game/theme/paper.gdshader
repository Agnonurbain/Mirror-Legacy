// Rice paper (Shuimo): a grain of fibres at three scales, long fibres laid in the pulp, flecks of gold leaf, and edges
// darkened by age.
shader_type canvas_item;

uniform vec4 paper : source_color = vec4(0.96, 0.93, 0.84, 1.0);
uniform vec4 gold : source_color = vec4(0.80, 0.62, 0.22, 1.0);
uniform float gold_density = 0.0035;
uniform vec4 fibre : source_color = vec4(0.55, 0.45, 0.30, 1.0);

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
    vec2 p = FRAGCOORD.xy;
    float grain = noise(p * 0.8) * 0.5 + noise(p * 0.25) * 0.35 + noise(p * 0.05) * 0.15;
    float fibres = smoothstep(0.93, 1.0, noise(vec2(p.x * 0.02, p.y * 0.6))) * 0.5
        + smoothstep(0.95, 1.0, noise(vec2(p.x * 0.7, p.y * 0.015))) * 0.4;
    vec3 colour = paper.rgb * (0.95 + 0.07 * grain) - fibre.rgb * 0.06 * fibres;
    float age = smoothstep(0.35, 0.85, length((UV - 0.5) * vec2(1.0, 1.2)));
    colour = mix(colour, colour * 0.82 + vec3(0.04, 0.03, 0.0), age * 0.6);
    // gold leaf: rare cells hold a torn fleck, its size and shade their own
    vec2 cell = floor(p / 14.0);
    if (hash(cell) < gold_density * 14.0)
    {
        vec2 centre = (cell + vec2(hash(cell + 1.7), hash(cell + 3.1))) * 14.0;
        float radius = 1.2 + 2.8 * hash(cell + 5.3);
        float torn = length(p - centre) + noise(p * 0.9) * 1.6;
        float fleck = smoothstep(radius, radius - 1.0, torn);
        colour = mix(colour, gold.rgb * (0.85 + 0.3 * hash(cell + 7.9)), fleck * 0.85);
    }
    COLOR = vec4(colour, 1.0);
}
