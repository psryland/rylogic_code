import * as Rdr_ from "../renderer";

/** Shader types */
export enum EShaderType
{
	VS = WebGLRenderingContext.VERTEX_SHADER,
	PS = WebGLRenderingContext.FRAGMENT_SHADER,
}

/** A component of a shader program */
export interface IShader
{
	/** The shader type */
	type: EShaderType;

	/** The GL shader */
	shdr: WebGLShader;
}

/** A complete shader program */
export interface IShaderProgram
{
	/** The WebGL shader program */
	program: WebGLProgram;

	/** Stream attributes */
	position: number;
	normal: number;
	colour: number;
	texcoord: number;

	/** Transform constants */
	o2w: WebGLUniformLocation;
	w2c: WebGLUniformLocation;
	c2s: WebGLUniformLocation;

	/** Tinting constants */
	tint:
		{
			colour: WebGLUniformLocation;
		};

	/** Camera constants */
	camera:
		{
			ws_position: WebGLUniformLocation;
			ws_forward: WebGLUniformLocation;
		};

	/** Texture constants */
	tex_diffuse:
		{
			enabled: WebGLUniformLocation;
			sampler: WebGLUniformLocation;
		};

	/** Lighting constants */
	light:
		{
			lighting_type: WebGLUniformLocation,
			position: WebGLUniformLocation,
			direction: WebGLUniformLocation,
			ambient: WebGLUniformLocation,
			diffuse: WebGLUniformLocation,
			specular: WebGLUniformLocation,
			specpwr: WebGLUniformLocation,
		};
}

/**
 * Create a new shader component
 * @param rdr The main renderer instance
 * @param shader_type The type of shader component being created
 * @param source The source code for the shader component
 */
export function CreateShader(rdr: Rdr_.Renderer, shader_type: EShaderType, source: string): IShader
{
	let gl = rdr.webgl;

	// Create a shader instance
	let shader = <WebGLShader>gl.createShader(shader_type);

	// Compile the shader
	gl.shaderSource(shader, source);
	gl.compileShader(shader);
	if (!gl.getShaderParameter(shader, gl.COMPILE_STATUS))
		throw new Error('Could not compile shader. \n\n' + gl.getShaderInfoLog(shader));

	// Return the new shader
	return { type: shader_type, shdr: shader };
}

/**
 * Create a shader program from a collection of shader components
 * @param rdr The main renderer instance
 * @param parts The shader components to add to the program
 */
export function CreateShaderProgram(rdr: Rdr_.Renderer, ...parts: IShader[]): IShaderProgram
{
	let gl = rdr.webgl;

	// Create an empty program
	let program = <WebGLProgram>gl.createProgram();

	// Add the shaders
	for (let s of parts)
		gl.attachShader(program, s.shdr);

	// Compile into a shader 'program'
	gl.linkProgram(program);
	if (!gl.getProgramParameter(program, gl.LINK_STATUS))
		throw new Error('Could not compile WebGL program. \n\n' + gl.getProgramInfoLog(program));

	// Save the program

	let shader_program:IShaderProgram =
		{
			program: program,

			// Read variables from the shaders
			position: gl.getAttribLocation(program, "position"),
			normal: gl.getAttribLocation(program, "normal"),
			colour: gl.getAttribLocation(program, "colour"),
			texcoord: gl.getAttribLocation(program, "texcoord"),

			o2w: <WebGLUniformLocation>gl.getUniformLocation(program, "o2w"),
			w2c: <WebGLUniformLocation>gl.getUniformLocation(program, "w2c"),
			c2s: <WebGLUniformLocation>gl.getUniformLocation(program, "c2s"),

			tint:
				{
					colour: <WebGLUniformLocation>gl.getUniformLocation(program, "tint_colour"),
				},

			camera:
				{
					ws_position: <WebGLUniformLocation>gl.getUniformLocation(program, "cam_ws_position"),
					ws_forward: <WebGLUniformLocation>gl.getUniformLocation(program, "cam_ws_forward"),
				},

			tex_diffuse:
				{
					enabled: <WebGLUniformLocation>gl.getUniformLocation(program, "has_tex_diffuse"),
					sampler: <WebGLUniformLocation>gl.getUniformLocation(program, "sampler_diffuse"),
				},

			light:
				{
					lighting_type: <WebGLUniformLocation>gl.getUniformLocation(program, "lighting_type"),
					position: <WebGLUniformLocation>gl.getUniformLocation(program, "light_position"),
					direction: <WebGLUniformLocation>gl.getUniformLocation(program, "light_direction"),
					ambient: <WebGLUniformLocation>gl.getUniformLocation(program, "light_ambient"),
					diffuse: <WebGLUniformLocation>gl.getUniformLocation(program, "light_diffuse"),
					specular: <WebGLUniformLocation>gl.getUniformLocation(program, "light_specular"),
					specpwr: <WebGLUniformLocation>gl.getUniformLocation(program, "light_specpwr"),
				}
		};

	return shader_program;
}

