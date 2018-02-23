export var source =
	`
attribute vec3 position;
attribute vec3 normal;
attribute vec4 colour;
attribute vec2 texcoord;

uniform mat4 o2w;
uniform mat4 w2c;
uniform mat4 c2s;

uniform vec4 tint_colour;

varying vec4 ps_ws_position;
varying vec4 ps_ws_normal;
varying vec4 ps_colour;
varying vec2 ps_texcoord;

void main(void)
{
    ps_ws_position = o2w * vec4(position, 1.0);
    ps_ws_normal = o2w * vec4(normal, 0.0);
    ps_colour =  colour * tint_colour;
    ps_texcoord = texcoord;
    gl_Position = c2s * w2c * ps_ws_position;
}
`;