#ifndef HLSL_IDENTIFIERS_FXH
#define HLSL_IDENTIFIERS_FXH

// Vertex shader identifiers **************************************

typedef int VertexDiffuse;
#define VertexDiffuse_None							0
#define VertexDiffuse_Amb							1
#define VertexDiffuse_Amb_p_Directional				2
#define VertexDiffuse_Amb_p_Point					3
#define VertexDiffuse_Directional					4
#define VertexDiffuse_Point							5

typedef bool VertexSpecular;
#define VertexSpecular_None							false
#define VertexSpecular_On							true

typedef bool TexCoordsDiffuse;
#define TexCoordsDiffuse_None						false
#define TexCoordsDiffuse_On							true

typedef bool TexCoordsEnviroMap;
#define TexCoordsEnviroMap_None						false
#define TexCoordsEnviroMap_On						true

typedef int VSDiffOut;
#define VSDiffOut_Zero								0
#define VSDiffOut_One								1
#define VSDiffOut_InDiff							200
#define VSDiffOut_InDiff_p_LtDiff					201
#define VSDiffOut_InDiff_x_LtDiff					202
#define VSDiffOut_LtDiff							400
#define VSDiffOut_LtDiff_p_LtSpec					401

// Pixel shader identifiers **************************************

typedef int PerPixelDiffuse;
#define PerPixelDiffuse_None						0
#define PerPixelDiffuse_Amb							1
#define PerPixelDiffuse_Amb_p_Directional			2
#define PerPixelDiffuse_Directional					3

typedef bool PerPixelSpecular;
#define PerPixelSpecular_None						false
#define PerPixelSpecular_On							true

typedef bool TexDiffuse;
#define TexDiffuse_None								false
#define TexDiffuse_On								true

typedef bool EnviroMap;
#define EnviroMap_None								false
#define EnviroMap_On								true

// Order is Amb, InDiff, InSpec, Tex, LtDiff, LtSpec
typedef int PSOut;
#define PSOut_Zero									0
#define PSOut_One									1
#define PSOut_Amb									100
#define PSOut_Amb_p_InDiff							101
#define PSOut_Amb_p_Tex								102
#define PSOut_Amb_x_Tex								103
#define PSOut_Amb_x_Tex_p_LtSpec					104
#define PSOut_InDiff								200
#define PSOut_InDiff_p_Tex							201
#define PSOut_InDiff_x_Tex							202
#define PSOut_InDiff_x_Tex_p_InSpec					203
#define PSOut_InDiff_p_Tex_x_LtDiff					204
#define PSOut_InDiff_x_Tex_p_LtSpec					205
#define PSOut_InDiff_x_Tex_x_LtDiff					206
#define PSOut_InDiff_x_Tex_x_LtDiff_p_InSpec		207
#define PSOut_InDiff_x_Tex_x_LtDiff_p_LtSpec		208
#define PSOut_InDiff_p_LtDiff						209
#define PSOut_InDiff_x_LtDiff						210
#define PSOut_InDiff_x_LtDiff_p_LtSpec				211
#define PSOut_InDiff_p_InSpec						212
#define PSOut_InSpec								300
#define PSOut_Tex									400
#define PSOut_Tex_x_LtDiff							401
#define PSOut_Tex_x_LtDiff_p_LtSpec					402
#define PSOut_Tex_p_LtSpec							403
#define PSOut_LtDiff								500
#define PSOut_LtDiff_p_LtSpec						501
#define PSOut_LtSpec								600

#endif//HLSL_IDENTIFIERS_FXH