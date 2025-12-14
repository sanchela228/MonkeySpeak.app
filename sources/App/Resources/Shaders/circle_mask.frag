#version 330

in vec2 fragTexCoord;
out vec4 finalColor;

uniform sampler2D texture0;
uniform vec2 resolution;

void main()
{
    vec2 uv = fragTexCoord * resolution;
    vec2 center = resolution * 0.5;

    float dist = length(uv - center);
    float radius = min(resolution.x, resolution.y) * 0.5;

    if (dist > radius)
    discard;

    finalColor = texture(texture0, fragTexCoord);
}
