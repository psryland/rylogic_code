//***********************************************
// Renderer
//  Copyright © Rylogic Ltd 2010
//***********************************************

#include "../types.hlsli"

// Returns the intensity of reflected directional light on a surface with normal 'ws_norm' and transparency 'alpha'
float LightDirectional(in uniform float4 ws_light_direction, in float4 ws_norm, in float alpha)
{
	float brightness = -dot(ws_light_direction, ws_norm);
	return lerp(saturate(brightness), (1.0 - alpha) * abs(brightness), 1.0 - alpha);
}

// Returns the intensity of reflected radial light on a surface at position 'ws_pos' with normal 'ws_norm' and transparency 'alpha'
float LightPoint(in uniform float4 ws_light_position, in float4 ws_norm, in float4 ws_pos, in float alpha)
{
	float4 light_to_pos = ws_pos - ws_light_position;
	float dist = length(light_to_pos);
	float brightness = -dot(light_to_pos, ws_norm) / dist;
	return lerp(saturate(brightness), (1.0 - alpha) * abs(brightness), 1.0 - alpha);
}

// Returns the intensity of reflected radial light on a surface at position 'ws_pos' with normal 'ws_norm' and transparency 'alpha'
float LightSpot(in uniform float4 ws_light_position, in uniform float4 ws_light_direction, in uniform float inner_cosangle, in uniform float outer_cosangle, in uniform float range, in float4 ws_norm, in float4 ws_pos, in float alpha)
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
float LightSpecular(in uniform float4 ws_light_direction, in uniform float specular_power, in float4 ws_norm, in float4 ws_toeye_norm, in float alpha)
{
	float4 ws_H = normalize(ws_toeye_norm - ws_light_direction);
	float brightness = dot(ws_norm, ws_H);
	brightness = lerp(saturate(brightness), (1.0 - alpha) * abs(brightness), 1.0 - alpha);
	return pow(saturate(brightness), specular_power);
}

// Return the colour due to lighting. Returns unlit_diff if ws_norm is zero
float4 Illuminate(in uniform Light light, float4 ws_pos, float4 ws_norm, float4 ws_cam, float light_visible, float4 unlit_diff)
{
	// Lighting should not change the alpha value. If the thing was semi transparent coming in, casting
	// light on it shouldn't change it.
	float  has_norm      = dot(ws_norm,ws_norm); // 1 for normals, 0 for not
	float4 ws_toeye_norm = normalize(ws_cam - ws_pos);
	float4 ws_light_dir  = DirectionalLight(light) ? light.m_ws_direction : normalize(ws_pos - light.m_ws_position);

	float intensity = 0;
	if      (DirectionalLight(light)) intensity = LightDirectional(light.m_ws_direction ,ws_norm ,unlit_diff.a);
	else if (PointLight(light))       intensity = LightPoint      (light.m_ws_position  ,ws_norm ,ws_pos ,unlit_diff.a);
	else if (SpotLight(light))        intensity = LightSpot       (light.m_ws_position  ,light.m_ws_direction ,light.m_spot.x ,light.m_spot.y ,light.m_spot.z ,ws_norm ,ws_pos ,unlit_diff.a);

	float4 ltdiff = float4(0,0,0,0);
	ltdiff.rgb += light.m_ambient.rgb;
	ltdiff.rgb += intensity * light_visible * light.m_colour.rgb;
	ltdiff.rgb  = 2.0 * (ltdiff.rgb - 0.5) * unlit_diff.rgb;
	ltdiff.rgb *= has_norm;

	float4 ltspec = float4(0,0,0,0);
	ltspec.rgb += intensity * light_visible * light.m_specular.rgb * LightSpecular(ws_light_dir ,light.m_specular.a ,ws_norm ,ws_toeye_norm ,unlit_diff.a);
	ltspec.rgb *= has_norm;

	return saturate(ltdiff + ltspec + unlit_diff);
}
