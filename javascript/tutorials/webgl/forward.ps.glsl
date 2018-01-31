let forward_ps = `
precision mediump float;

uniform vec3 cam_ws_position;
uniform vec3 cam_ws_forward;

uniform bool has_tex_diffuse;
uniform sampler2D sampler_diffuse;

uniform int lighting_type; // 0 = no, 1 = directional, 2 = radial, 3 = spot
uniform vec3 light_position;
uniform vec3 light_direction;
uniform vec3 light_ambient;
uniform vec3 light_diffuse;
uniform vec3 light_specular;
uniform float light_specpwr;

varying vec4 ps_ws_position;
varying vec4 ps_ws_normal;
varying vec4 ps_colour;
varying vec2 ps_texcoord;

vec4 Illuminate(vec4 ws_pos, vec4 ws_norm, vec4 diff);

void main(void)
{
	vec4 ws_pos = ps_ws_position;
	vec4 ws_norm = normalize(ps_ws_normal);
	vec4 diff = ps_colour;
	
	if (has_tex_diffuse)
		diff = diff * texture2D(sampler_diffuse, ps_texcoord);

	if (lighting_type != 0)
		diff = Illuminate(ws_pos, ws_norm, diff);

	gl_FragColor = diff;
}

// Directional light intensity
float LightDirectional(vec4 ws_norm, float alpha)
{
	float brightness = -dot(light_direction, ws_norm.xyz);
	return mix(clamp(brightness, 0.0, 1.0), (1.0 - alpha) * abs(brightness), 1.0 - alpha);
}

// Specular light intensity
float LightSpecular(vec4 ws_norm, vec4 ws_toeye_norm, float alpha)
{
	vec4 ws_H = normalize(ws_toeye_norm - vec4(light_direction, 0));
	float brightness = dot(ws_norm, ws_H);
	brightness = mix(clamp(brightness, 0.0, 1.0), (1.0 - alpha) * abs(brightness), 1.0 - alpha);
	return pow(clamp(brightness, 0.0, 1.0), light_specpwr);
}

// Illuminate 'diff'
vec4 Illuminate(vec4 ws_pos, vec4 ws_norm, vec4 diff)
{
	float intensity =
		lighting_type == 1 ? LightDirectional(ws_norm, diff.a) :
		lighting_type == 2 ? 0.0 :
		lighting_type == 3 ? 0.0 :
		0.0;

	vec4 ltdiff = vec4(0,0,0,0);
	ltdiff.rgb += light_ambient.rgb;
	ltdiff.rgb += intensity * light_diffuse.rgb;
	ltdiff.rgb  = 2.0 * (ltdiff.rgb - 0.5) * diff.rgb;

	vec4 ltspec = vec4(0,0,0,0);
	ltspec.rgb += intensity * light_specular.rgb * LightSpecular(ws_norm, normalize(vec4(cam_ws_position, 1) - ws_pos), diff.a);

	//return vec4(ltdiff.rgb, diff.a);
	return clamp(diff + ltdiff + ltspec, 0.0, 1.0);
}
`