//************************************************************
//
//	Common - Variable definitions for shaders
//
//************************************************************
#ifndef COMMON_FXH
#define COMMON_FXH

shared float4x4	g_object_to_world		: World;
shared float4x4	g_object_to_screen		: WorldViewProjection;
shared float4x4	g_camera_to_world		: ViewInverse;
//shared float4x4	g_world_to_camera		: View;
//shared float4x4	g_object_to_camera		: WorldView;
//shared float4x4	g_world_to_screen		: ViewProjection;
//shared float4x4	g_camera_to_screen		: Projection;

float4 WSCameraPosition()
{
	return g_camera_to_world[3];
}

float4 ObjectToWorld(float4 os_vec)
{
	return mul(os_vec, g_object_to_world);
}

float4 ObjectToScreen(float4 os_vec)
{
	return mul(os_vec, g_object_to_screen);
}

//float4x4	ObjectToCamera()
//{
//	return mul(g_object_to_world, g_world_to_camera);
//}
//
//float4x4	CameraToWorld()
//{
//	return mul(g_object_to_world, g_world_to_camera);
//}

#endif//COMMON_FXH

