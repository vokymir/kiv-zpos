#version 330 core

in vec3 vNearPoint;
in vec3 vFarPoint;

uniform mat4  uViewProjection;
uniform vec3  uCameraPos;
uniform float uFadeStart;
uniform float uFadeEnd;

out vec4 FragColor;

float gridLine(float coord, float lineWidth)
{
	float f  = abs(fract(coord - 0.5) - 0.5);
	float df = fwidth(coord);
	return 1.0 - smoothstep(lineWidth - df, lineWidth + df, f);
}

void main()
{
	// Intersect ray with Y=0 plane
	float t = -vNearPoint.y / (vFarPoint.y - vNearPoint.y);
	if (t <= 0.0)
		discard;

	vec3 worldPos = vNearPoint + t * (vFarPoint - vNearPoint);

	// Write correct depth so the grid is occluded by geometry
	vec4 clip     = uViewProjection * vec4(worldPos, 1.0);
	gl_FragDepth  = (clip.z / clip.w) * 0.5 + 0.5;

	float dist  = length(worldPos.xz - uCameraPos.xz);
	float alpha = 1.0 - smoothstep(uFadeStart, uFadeEnd, dist);
	if (alpha <= 0.0)
		discard;

	// Minor grid (1-unit) and major grid (10-unit)
	float minor = max(gridLine(worldPos.x,        0.02), gridLine(worldPos.z,        0.02));
	float major = max(gridLine(worldPos.x / 10.0, 0.015), gridLine(worldPos.z / 10.0, 0.015));

	float minorAlpha = minor * 0.35 * alpha * (1.0 - major);
	float majorAlpha = major * mix(0.15, 0.7, alpha);
	float lineAlpha  = max(minorAlpha, majorAlpha);

	if (lineAlpha < 0.01)
		discard;

	vec3 color = mix(vec3(0.224, 0.224, 0.224), vec3(0.32, 0.32, 0.32), major);
	FragColor = vec4(color, lineAlpha);
}
