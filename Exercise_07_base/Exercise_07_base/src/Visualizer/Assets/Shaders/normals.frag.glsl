#version 330 core

in vec3 vWorldNormal;

out vec4 FragColor;

void main()
{
	// Remap normal from [-1, 1] to [0, 1] for color visualization
	vec3 color = normalize(vWorldNormal) * 0.5 + 0.5;
	FragColor = vec4(color, 1.0);
}
