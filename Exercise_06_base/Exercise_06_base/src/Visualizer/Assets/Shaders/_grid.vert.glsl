#version 330 core

uniform mat4 uInvView;
uniform mat4 uInvProjection;
uniform mat4 uViewProjection;

out vec3 vNearPoint;
out vec3 vFarPoint;

// Full-screen triangle; no VBO needed
vec2 positions[3] = vec2[](
	vec2(-1.0, -1.0),
	vec2( 3.0, -1.0),
	vec2(-1.0,  3.0)
);

vec3 unproject(vec2 ndc, float ndcZ)
{
	vec4 clipPos  = vec4(ndc, ndcZ, 1.0);
	vec4 viewPos  = uInvProjection * clipPos;
	viewPos      /= viewPos.w;
	vec4 worldPos = uInvView * viewPos;
	return worldPos.xyz;
}

void main()
{
	vec2 ndc    = positions[gl_VertexID];
	vNearPoint  = unproject(ndc, -1.0);
	vFarPoint   = unproject(ndc,  1.0);
	gl_Position = vec4(ndc, 0.0, 1.0);
}
