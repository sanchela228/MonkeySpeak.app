#version 330

in vec2 fragTexCoord;
in vec4 fragColor;

uniform sampler2D texture0;
uniform vec2 resolution;
uniform float blurStrength;

out vec4 finalColor;

void main()
{
    vec2 texelSize = 1.0 / resolution;
    vec4 color = vec4(0.0);
    float total = 0.0;
    int radius = 5;

    for (int x = -radius; x <= radius; x++)
    {
        for (int y = -radius; y <= radius; y++)
        {
            float weight = exp(-(x*x + y*y) / (2.0 * blurStrength * blurStrength));
            color += texture(texture0, fragTexCoord + vec2(x, y) * texelSize) * weight;
            total += weight;
        }
    }

    color /= total;
    finalColor = color * fragColor;
}
