import * as Math_ from "../maths/maths";
import * as M4x4_ from "../maths/m4x4";
import * as Vec4_ from "../maths/v4";
import * as Camera from "./camera/camera";
import * as Model from "./model/model";
import * as Instance from "./model/instance";
import * as Texture from "./model/texture";
import * as Shader from "./shaders/shader";
import * as Light from "./shaders/light";
import * as Canvas from "./canvas/canvas";
import * as FwdVS from "./shaders/forward.vs";
import * as FwdPS from "./shaders/forward.ps";
import Vec4 = Math_.Vec4;
import ICamera = Camera.ICamera;

export
{
	Camera,
	Instance,
	Model,
	Texture,
	Light,
	Shader,
	Canvas,
}

/** Geometry type enumeration */
export enum EGeom
{
	None = 0,
	Vert = 1 << 0,
	Colr = 1 << 1,
	Norm = 1 << 2,
	Tex0 = 1 << 3,
}

/** The primitive type of the geometry in a nugget */
export enum ETopo
{
	Invalid = 0,
	None = 0,
	PointList = WebGLRenderingContext.POINTS,
	LineList = WebGLRenderingContext.LINES,
	LineStrip = WebGLRenderingContext.LINE_STRIP,
	TriList = WebGLRenderingContext.TRIANGLES,
	TriStrip = WebGLRenderingContext.TRIANGLE_STRIP,
}

/** Light source type enumeration */
export enum ELight
{
	Ambient     = 0,
	Directional = 1,
	Radial      = 2,
	Spot        = 3,
}

/** Rendering control flags */
export enum EFlags
{
	None = 0,

	// The object is hidden
	Hidden = 1 << 0,

	// The object is filled in wireframe mode
	Wireframe = 1 << 1,

	// Render the object without testing against the depth buffer
	NoZTest = 1 << 2,

	// Render the object without effecting the depth buffer
	NoZWrite = 1 << 3,

	// Set when an object is selected. The meaning of 'selected' is up to the application
	Selected = 1 << 8,

	// Doesn't contribute to the bounding box on an object.
	BBoxExclude = 1 << 9,

	// Should not be included when determining the bounds of a scene.
	SceneBoundsExclude = 1 << 10,

	// Ignored for hit test ray casts
	HitTestExclude = 1 << 11,
}

/** Renderer */
export class Renderer
{
	/**
	 * Create a renderer instance for a canvas element.
	 * @param canvas The html canvas element to render on.
	 */
	constructor(canvas: HTMLCanvasElement)
	{
		// Get the rendering context from the canvas
		this.webgl = <WebGLRenderingContext>canvas.getContext("experimental-webgl", { antialias: true, depth: true });
		if (!this.webgl)
			throw new Error("WebGL is not available");

		// Set the initial viewport dimensions
		this.webgl.viewport(0, 0, canvas.width, canvas.height);

		// Set the default back colour (r,g,b,a)
		this.back_colour = Vec4_.create(0.5, 0.5, 0.5, 1.0);

		// Create stock textures.
		this.textures = Texture.CreateStockTextures(this);

		// Initialise the standard forward rendering shader
		// Component shaders are stored in 'shaders'
		// Compiled shader programs are stored in 'shader_programs
		// User code can do similar to add new shader programs constructed
		// from existing or new shader components.
		this.shaders = {};
		this.shaders.forward_vs = Shader.CreateShader(this, Shader.EShaderType.VS, FwdVS.source);
		this.shaders.forward_ps = Shader.CreateShader(this, Shader.EShaderType.PS, FwdPS.source);

		// Initialise the shader programs
		this.shader_programs = {};
		this.shader_programs.forward = Shader.CreateShaderProgram(this,
			this.shaders.forward_vs,
			this.shaders.forward_ps);

		// Viewport dimensions
		this._bb_width  = 0;
		this._bb_height = 0;
	}

	/** Get the web GL rendering context used by this renderer */
	public webgl: WebGLRenderingContext;

	/** The background clear colour */
	public back_colour: Vec4;

	/** Compiled shaders */
	public shaders: { [_: string]: Shader.IShader };

	/** Programs of combined shaders */
	public shader_programs: { [_: string]: Shader.IShaderProgram };

	/** Texture instances */
	public textures: { [_: string]: Texture.ITexture; };

	/** Get the view aspect ratio */
	get aspect(): number
	{
		let canvas = this.webgl.canvas;
		return canvas.width / canvas.height;
	}

	/**
	 * Render a scene
	 * @param instances The instances that make up the scene.
	 * @param camera The view into the scene
	 * @param global_light The global light source
	 */
	Render(instances: Instance.IInstance[], camera: ICamera, global_light: Light.ILight)
	{
		let gl = this.webgl;

		// Build a nugget draw list
		let drawlist = this._BuildDrawList(instances);

		// Handle resize
		this._ResizeIfNeeded();

		// Clear the back/depth buffer
		gl.clearColor(this.back_colour[0], this.back_colour[1], this.back_colour[2], this.back_colour[3]);
		gl.clear(gl.COLOR_BUFFER_BIT | gl.DEPTH_BUFFER_BIT);

		// Render each nugget
		for (let i = 0; i != drawlist.length; ++i)
		{
			var dle = drawlist[i];
			let nug = dle.nug;
			let inst = dle.inst;
			let model = inst.model;
			let shader = nug.shader;

			// Bind the shader to the gfx card
			gl.useProgram(shader.program);

			// Set render states
			{
				// Z Test
				if ((inst.flags & EFlags.NoZTest) == 0)
					gl.enable(gl.DEPTH_TEST);
				else
					gl.disable(gl.DEPTH_TEST);

				// Z Write
				if ((inst.flags & EFlags.NoZWrite) == 0)
					gl.depthMask(true);
				else
					gl.depthMask(false);
			}

			// Bind shader input streams
			{
				// Bind vertex buffer
				if (model.geom & EGeom.Vert && model.vbuffer)
				{
					gl.bindBuffer(gl.ARRAY_BUFFER, model.vbuffer.data);
					gl.vertexAttribPointer(shader.position, model.vbuffer.stride, gl.FLOAT, false, 0, 0);
					gl.enableVertexAttribArray(shader.position);
				}
				else if (shader.position != -1)
				{
					gl.vertexAttrib3fv(shader.colour, [0, 0, 0]);
					gl.disableVertexAttribArray(shader.position);
				}

				// Bind normals buffer
				if (model.geom & EGeom.Norm && model.nbuffer)
				{
					gl.bindBuffer(gl.ARRAY_BUFFER, model.nbuffer.data);
					gl.vertexAttribPointer(shader.normal, model.nbuffer.stride, gl.FLOAT, false, 0, 0);
					gl.enableVertexAttribArray(shader.normal);
				}
				else if (shader.normal != -1)
				{
					gl.vertexAttrib3fv(shader.normal, [0, 0, 0]);
					gl.disableVertexAttribArray(shader.normal);
				}

				// Bind colours buffer
				if (model.geom & EGeom.Colr && model.cbuffer)
				{
					gl.bindBuffer(gl.ARRAY_BUFFER, model.cbuffer.data);
					gl.vertexAttribPointer(shader.colour, model.cbuffer.stride, gl.FLOAT, false, 0, 0);
					gl.enableVertexAttribArray(shader.colour);
				}
				else if (shader.colour != -1)
				{
					gl.vertexAttrib3fv(shader.colour, [1, 1, 1]);
					gl.disableVertexAttribArray(shader.colour);
				}

				// Bind texture coords buffer
				if (model.geom & EGeom.Tex0 && model.tbuffer)
				{
					gl.bindBuffer(gl.ARRAY_BUFFER, model.tbuffer.data);
					gl.vertexAttribPointer(shader.texcoord, model.tbuffer.stride, gl.FLOAT, false, 0, 0);
					gl.enableVertexAttribArray(shader.texcoord);
				}
				else if (shader.texcoord != -1)
				{
					gl.vertexAttrib2fv(shader.texcoord, [0, 0]);
					gl.disableVertexAttribArray(shader.texcoord);
				}
			}

			// Bind the diffuse texture
			{
				// Get the texture to bind, or the default texture if not ready yet
				var texture = (nug.tex_diffuse && nug.tex_diffuse.loaded) ? nug.tex_diffuse : this.textures.white;

				// Set the enable texture flag
				if (shader.tex_diffuse.enabled != -1)
					gl.uniform1i(shader.tex_diffuse.enabled, nug.tex_diffuse ? +1 : 0);

				// Set the texture unit
				if (shader.tex_diffuse.sampler != -1)
					gl.uniform1i(shader.tex_diffuse.sampler, 0);

				// Bind the texture
				gl.bindTexture(gl.TEXTURE_2D, texture.tex);
				gl.activeTexture(gl.TEXTURE0);
			}

			// Set lighting
			{
				// Use the global light, or one from the nugget
				let light = nug.light || global_light;
				if (model.geom & EGeom.Norm && light)
				{
					gl.uniform1i(shader.light.lighting_type, light.lighting_type);
					gl.uniform3fv(shader.light.position    , (<Float32Array>light.position).slice(0,3));
					gl.uniform3fv(shader.light.direction   , (<Float32Array>light.direction).slice(0,3));
					gl.uniform3fv(shader.light.ambient     , (<Float32Array>light.ambient).slice(0,3));
					gl.uniform3fv(shader.light.diffuse     , (<Float32Array>light.diffuse).slice(0,3));
					gl.uniform3fv(shader.light.specular    , (<Float32Array>light.specular).slice(0,3));
					gl.uniform1f(shader.light.specpwr      , light.specpwr);
				}
				else if (shader.light.lighting_type != -1)
				{
					gl.uniform1i(shader.light.lighting_type, 0);
				}
			}

			// Set tint
			{
				let tint = inst.tint || nug.tint || Vec4_.create(1, 1, 1, 1);
				gl.uniform4fv(shader.tint.colour, <Float32Array>tint);
			}

			// Set transforms
			{
				// Set the projection matrix
				let c2s = nug.c2s || camera.c2s;
				gl.uniformMatrix4fv(shader.c2s, false, <Float32Array>c2s);

				// Set the camera matrix
				gl.uniformMatrix4fv(shader.w2c, false, <Float32Array>camera.w2c);
				gl.uniform3fv(shader.camera.ws_position, (<Float32Array>camera.pos).slice(0,3));
				gl.uniform3fv(shader.camera.ws_forward, (<Float32Array>camera.fwd).slice(0,3));

				// Set the o2w transform
				gl.uniformMatrix4fv(shader.o2w, false, <Float32Array>inst.o2w);
			}

			// Render the nugget
			{
				// If the model has no index buffer, draw the vertices only
				if (model.ibuffer)
				{
					// Get the buffer range to render
					let irange = nug.irange || { ofs: 0, count: model.ibuffer.count };
					gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, model.ibuffer.data);
					gl.drawElements(nug.topo, irange.count, gl.UNSIGNED_SHORT, irange.ofs * 2); // offset in bytes
				}
				else if (model.vbuffer)
				{
					// Get the buffer range to render
					let vrange = nug.vrange || { ofs: 0, count: model.vbuffer.count };
					gl.drawArrays(nug.topo, vrange.ofs, vrange.count);
				}
			}

			// Clean up
			gl.bindTexture(gl.TEXTURE_2D, null);
			gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, null);
			gl.bindBuffer(gl.ARRAY_BUFFER, null);
		}
	}

	/**
	 * Resize the back buffer if the canvas size has changed
	 */
	_ResizeIfNeeded(): void
	{
		var canvas = this.webgl.canvas;

		// Unchanged?
		if (canvas.width == this._bb_width &&
			canvas.height == this._bb_height)
			return;

		this._bb_width = canvas.width;
		this._bb_height = canvas.height;

		// Calculate the physical pixel sizes so we set the viewport to be 
		var pixel_ratio = window.devicePixelRatio ? window.devicePixelRatio : 1.0;
		var physical_w = Math.floor(canvas.width * pixel_ratio);
		var physical_h = Math.floor(canvas.height * pixel_ratio);

		// Set the new viewport dimensions.
		// drawingBufferWidth/Height should match the canvas width/height
		this.webgl.viewport(0, 0, physical_w, physical_h);
	}
	private _bb_width : number;
	private _bb_height: number;

	/**
	 * Returns an ordered list of nuggets to render
	 * @param instances The instances to build the drawlist from.
	 * @returns An ordered list of draw list elements to render
	 */
	_BuildDrawList(instances: Instance.IInstance[]): { nug: Model.INugget; inst: Instance.IInstance; }[]
	{
		let drawlist: { nug: Model.INugget; inst: Instance.IInstance; }[] = [];
		for (let i = 0; i != instances.length; ++i)
		{
			let inst = instances[i];

			// No model? skip...
			if (!inst.model)
				continue;

			// Create a draw list element for each model nugget
			for (let j = 0; j != inst.model.gbuffer.length; ++j)
			{
				let nug = inst.model.gbuffer[j];
				drawlist.push({ nug: nug, inst: inst });
			}
		}
		return drawlist;
	}
}

/**
 * Create a demo scene to show everything is working
 * @param rdr The renderer instance to render the demo scene in
 */
export function CreateDemoScene(rdr: Renderer)
{
	// Add the test model to the list of instances
	let instances = [CreateTestModel(rdr)];

	// Create a camera
	let camera = Camera.Create(Math_.TauBy8, rdr.aspect);
	M4x4_.LookAt(Vec4_.create(4, 4, 10, 1), Vec4_.Origin, Vec4_.YAxis, camera.c2w);

	// Create a light source
	let light = Light.Create(ELight.Directional, Vec4_.create(), Vec4_.create(-0.5,-0.5,-1.0,0), [0.1,0.1,0.1], [0.5,0.5,0.5], [0.5,0.5,0.5], 100);

	// Render the demo scene
	rdr.Render(instances, camera, light);
}

/**
 * Create a test model instance
 * @param rdr The render instance to create the model for
 */
export function CreateTestModel(rdr: Renderer): Instance.IInstance
{
	let gl = rdr.webgl;

	// Use the standard forward render shader
	let shader = rdr.shader_programs.forward;

	// Pyramid model
	let model = Model.Create(rdr,
	[// verts
		{ pos: [+0.0, +1.0, +0.0], norm: [+0.00, +1.00, +0.00], col: [1, 0, 0, 1], tex0: [0.50, 1] },
		{ pos: [-1.0, -1.0, -1.0], norm: [-0.57, -0.57, -0.57], col: [0, 1, 0, 1], tex0: [0.00, 0] },
		{ pos: [+1.0, -1.0, -1.0], norm: [+0.57, -0.57, -0.57], col: [0, 0, 1, 1], tex0: [0.25, 0] },
		{ pos: [+1.0, -1.0, +1.0], norm: [+0.57, -0.57, +0.57], col: [1, 1, 0, 1], tex0: [0.50, 0] },
		{ pos: [-1.0, -1.0, +1.0], norm: [-0.57, -0.57, +0.57], col: [0, 1, 1, 1], tex0: [0.75, 0] },
	],
	[// indices
		0, 1, 2,
		0, 2, 3,
		0, 3, 4,
		0, 4, 1,
		4, 3, 2,
		2, 1, 4,
	],
	[// nuggets
		{ topo: gl.TRIANGLES, shader: shader }//, tex_diffuse: rdr.stock_textures.checker1 }
	]);

	let o2w = M4x4_.Translation([0, 0, 0, 1]);
	let inst = Instance.Create("pyramid", model, o2w);
	return inst;
}