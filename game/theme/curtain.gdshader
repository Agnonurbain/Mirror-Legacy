// The curtain between screens (Shuimo): two panels of gold-flecked paper slide in from the sides, meet on red seals,
// then part. progress: 0 open, 1 closed.
shader_type canvas_item;

uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform vec4 paper : source_color = vec4(0.88, 0.81, 0.62, 1.0);
uniform vec4 gold : source_color = vec4(0.78, 0.58, 0.18, 1.0);
uniform vec4 seal : source_color = vec4(0.70, 0.12, 0.10, 1.0);

float hash(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }

void fragment()
{
    float reach = 0.5 * progress;                 // each panel covers this much of the width
    bool left = UV.x < reach;
    bool right = UV.x > 1.0 - reach;
    if (!left && !right) discard;

    vec2 p = FRAGCOORD.xy;
    vec3 colour = paper.rgb * (0.94 + 0.08 * hash(floor(p / 3.0)));
    vec2 cell = floor(p / 11.0);
    if (hash(cell) < 0.06)                        // gold leaf, denser than on the page
    {
        vec2 centre = (cell + vec2(hash(cell + 1.7), hash(cell + 3.1))) * 11.0;
        float fleck = smoothstep(1.0 + 2.5 * hash(cell + 5.3), 0.4, length(p - centre));
        colour = mix(colour, gold.rgb, fleck * 0.9);
    }

    // a red seal on each panel's inner edge, at mid-height
    float edge = left ? reach : 1.0 - reach;
    vec2 local = vec2((UV.x - edge) * 1.78 * (left ? -1.0 : 1.0), UV.y - 0.5);
    vec2 box = abs(local - vec2(0.045, 0.0));
    if (box.x < 0.04 && box.y < 0.075)
    {
        colour = seal.rgb;
        if ((box.x > 0.031 && box.x < 0.035) || (box.y > 0.066 && box.y < 0.07)) colour = mix(seal.rgb, vec3(0.96, 0.9, 0.85), 0.8);
    }
    // a soft shadow where the panel's edge lies on the page
    float shade = smoothstep(0.0, 0.012, abs(UV.x - edge));
    COLOR = vec4(colour * (0.85 + 0.15 * shade), 1.0);
}
