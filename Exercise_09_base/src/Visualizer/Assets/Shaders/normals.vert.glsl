#version 330 core

layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec3 aNormal;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;
uniform mat4 uNormalMatrix;

uniform float uPointSize;

out vec3 vWorldNormal;

void main()
{
	vec4 worldPos = uModel * vec4(aPosition, 1.0);

	vWorldNormal = normalize(mat3(uNormalMatrix) * aNormal);

	gl_Position = uProjection * uView * worldPos;
	gl_PointSize = uPointSize;
}
