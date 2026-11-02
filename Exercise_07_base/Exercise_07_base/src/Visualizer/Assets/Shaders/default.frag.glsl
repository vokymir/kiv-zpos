#version 430 core

struct DirectionalLight
{
	vec3  color;
	float intensity;
	vec3  direction;
	float _pad;
};

layout(std430, binding = 0) readonly buffer DirectionalLightBlock
{
	DirectionalLight uDirectionalLights[];
};

uniform vec3  uTint;
uniform int   uRenderMode;

uniform vec3  uAmbientColor;
uniform float uAmbientIntensity;
uniform int   uDirectionalLightCount;

uniform vec3  uViewPos;
uniform float uShininess;
uniform float uSpecularStrength;
uniform vec3  uSpecularColor;

in vec3 vWorldPosition;
in vec3 vWorldNormal;

out vec4 FragColor;

void main()
{
	vec3 normal  = normalize(vWorldNormal);
	vec3 viewDir = normalize(uViewPos - vWorldPosition);

	vec3 lighting = uAmbientColor * uAmbientIntensity;

	for (int i = 0; i < uDirectionalLightCount; i++)
	{
		vec3  lightDir = normalize(-uDirectionalLights[i].direction);
		float ndotl    = max(dot(normal, lightDir), 0.0);

		vec3 diffuse = uDirectionalLights[i].color * uDirectionalLights[i].intensity * ndotl;

		vec3  halfDir  = normalize(lightDir + viewDir);
		float spec     = pow(max(dot(normal, halfDir), 0.0), uShininess);
		vec3  specular = uSpecularColor * uDirectionalLights[i].color * uDirectionalLights[i].intensity * spec * uSpecularStrength;

		lighting += diffuse + specular;
	}

	if (uRenderMode == 2)
	{
		vec2 p = gl_PointCoord * 2.0 - 1.0;
		if (dot(p, p) > 1.0)
			discard;
	}

	FragColor = vec4(uTint * lighting, 1.0);
}
