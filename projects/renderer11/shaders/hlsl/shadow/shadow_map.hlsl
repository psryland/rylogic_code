//***********************************************
// Renderer
//  Copyright © Rylogic Ltd 2014
//***********************************************

#include "shadow_map_cbuf.hlsli"
#include "../inout.hlsli"

#define TINY 0.0001f

// PS input format
struct PSIn_ShadowMap
{
	// The positions of the vert projected onto each face of the frustum
	float4 ss_vert :SV_Position; // xy = unnormalised texture xy, zw = depth
	float4 xyz_face :TexCoord0; // GS: ws_vert PS: Normalised screen xyz and face index
};

struct PSOut
{
	// Expects a 2-channel texture, r = depth on wedge end of frustum, g = depth on far plane
	float2 depth :SV_Target;
};

// Vertex shader
#ifdef PR_RDR_VSHADER_shadow_map
PSIn_ShadowMap main(VSIn In)
{
	// Pass the ws_vert to the GS
	PSIn_ShadowMap Out;
	Out.ss_vert = float4(0,0,0,0);
	Out.xyz_face = mul(In.vert, m_o2w); // Pass ws_vert to GS
	return Out;
}
#endif

// Geometry shader
#ifdef PR_RDR_GSHADER_shadow_map_face
[maxvertexcount(15)]
void main(triangle PSIn_ShadowMap In[3], inout TriangleStream<PSIn_ShadowMap> TriStream)
{
	// Replicate the face for each projection and
	// project it onto the plane of the frustum
	PSIn_ShadowMap Out;
	for (int i = 0; i != 5; ++i)
	{
		if (!any(m_proj[i])) continue;
		for (int j = 0; j != 3; ++j)
		{
			Out = In[j];
			Out.ss_vert = mul(In[j].xyz_face, m_proj[i]);
			Out.xyz_face = float4(Out.ss_vert.xyz, i + TINY);
			TriStream.Append(Out);
		}
		TriStream.RestartStrip();
	}
}
#endif
#ifdef PR_RDR_GSHADER_shadow_map_line
[maxvertexcount(10)]
void main(line PSIn_ShadowMap In[2], inout TriangleStream<PSIn_ShadowMap> TriStream)
{
	// Replicate the line for each projection and
	// project it onto the plane of the frustum
	PSIn_ShadowMap Out;
	for (int i = 0; i != 5; ++i)
	{
		if (!any(m_proj[i])) continue;
		for (int j = 0; j != 2; ++j)
		{
			Out = In[j];
			Out.ss_vert = mul(In[j].xyz_face, m_proj[i]);
			Out.xyz_face = float4(Out.ss_vert.xyz, i + TINY);
			TriStream.Append(Out);
		}
		TriStream.RestartStrip();
	}
}
#endif

// Pixel shader
#ifdef PR_RDR_PSHADER_shadow_map
PSOut main(PSIn_ShadowMap In)
{
	float3 px = In.xyz_face.xyz / In.ss_vert.w;
	int face = int(In.xyz_face.w);

	// Clip to the wedge of the fwd texture we're rendering to (or no clip for the back texture)
	const float face_sign0[] = {-1.0f,  1.0f, -1.0f,  1.0f, 0.0f};
	const float face_sign1[] = { 1.0f, -1.0f, -1.0f,  1.0f, 0.0f};
	clip(face_sign0[face] * (px.y - px.x) + TINY);
	clip(face_sign1[face] * (px.y + px.x) + TINY);

	// Output the depth in the appropriate position in the RT
	PSOut Out;
	Out.depth = float2((face != 4) * px.z, (face == 4) * px.z);
	return Out;
}
#endif









