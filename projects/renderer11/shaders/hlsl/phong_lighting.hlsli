//***********************************************
// Renderer
//  Copyright � Rylogic Ltd 2010
//***********************************************
#ifndef PR_RDR_SHADER_PHONG_LIGHTING_HLSLI
#define PR_RDR_SHADER_PHONG_LIGHTING_HLSLI
#if SHADER_BUILD

#define DIRECTIONAL_LIGHT (m_light_info.x == 1.0)
#define POINT_LIGHT       (m_light_info.x == 2.0)
#define SPOT_LIGHT        (m_light_info.x == 3.0)

float LightDirectional(in float4 ws_light_direction, in float4 ws_norm, in float alpha)
{
	float brightness = -dot(ws_light_direction, ws_norm);
	if (brightness < 0.0) brightness = (1.0 - alpha) * abs(brightness);
	return saturate(brightness);
}

float LightPoint(in float4 ws_light_position, in float4 ws_norm, in float4 ws_pos, in float alpha)
{
	float4 light_to_pos = ws_pos - ws_light_position;
	float dist = length(light_to_pos);
	float brightness = -dot(light_to_pos, ws_norm) / dist;
	if (brightness < 0.0) brightness = (1.0 - alpha) * abs(brightness);
	return saturate(brightness);
}

float LightSpot(in float4 ws_light_position, in float4 ws_light_direction, in float inner_cosangle, in float outer_cosangle, in float range, in float4 ws_norm, in float4 ws_pos, in float alpha)
{
	float brightness = LightPoint(ws_light_position, ws_norm, ws_pos, alpha);
	float4 light_to_pos = ws_pos - ws_light_position;
	float dist = length(light_to_pos);
	float cos_angle = saturate(dot(light_to_pos, ws_light_direction) / dist);
	brightness *= saturate((outer_cosangle - cos_angle) / (outer_cosangle - inner_cosangle));
	brightness *= saturate((range - dist) * 9 / range);
	return brightness;
}

// Returns the intensity of specular light for a given incident light direction and surface normal
float LightSpecular(in float4 ws_light_direction, in float specular_power, in float4 ws_norm, in float4 ws_toeye_norm, in float alpha)
{
	float4 ws_H = normalize(ws_toeye_norm - ws_light_direction);
	float brightness = dot(ws_norm, ws_H);
	if (brightness < 0.0) brightness = (1.0 - alpha) * abs(brightness);
	return pow(saturate(brightness), specular_power);
}

// Return the colour due to lighting. Returns unlit_diff if ws_norm is zero
float4 Illuminate(float4 ws_pos, float4 ws_norm, float4 ws_cam, float4 unlit_diff)
{
	float  has_norm = dot(ws_norm,ws_norm); // 1 for normals, 0 for not
	float4 ltdiff = 0;
	float4 ltspec = 0;

	float4 ws_toeye_norm = normalize(ws_cam - ws_pos);
	float4 ws_light_dir  = DIRECTIONAL_LIGHT ? m_ws_light_direction : normalize(ws_pos - m_ws_light_position);

	float intensity = 0;
	if      (DIRECTIONAL_LIGHT) intensity = LightDirectional(m_ws_light_direction ,ws_norm         ,unlit_diff.a);
	else if (POINT_LIGHT)       intensity = LightPoint      (m_ws_light_position  ,ws_norm ,ws_pos ,unlit_diff.a);
	else if (SPOT_LIGHT)        intensity = LightSpot       (m_ws_light_position  ,m_ws_light_direction ,m_spot.x ,m_spot.y ,m_spot.z ,ws_norm ,ws_pos ,unlit_diff.a);

	ltdiff += m_light_ambient;
	ltdiff += intensity * m_light_colour;
	ltdiff  = 2.0 * (ltdiff - 0.5) * unlit_diff;
	ltdiff *= has_norm;

	ltspec += intensity * float4(m_light_specular.rgb,1) * LightSpecular(ws_light_dir ,m_light_specular.a ,ws_norm ,ws_toeye_norm ,unlit_diff.a);
	ltspec *= has_norm;

	return saturate(ltdiff + ltspec + unlit_diff);
}

#endif
#endif