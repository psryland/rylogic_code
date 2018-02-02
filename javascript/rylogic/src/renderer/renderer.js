import * as v4 from "../maths/v4";
import * as m4x4 from "../maths/m4x4";
import * as Maths from "../maths/maths";
import * as Model from "./model/model"
import * as Instance from "./model/instance"
import * as Texture from "./model/texture"
import * as Light from "./shaders/light"
import * as FwdVS from "./shaders/forward.vs"
import * as FwdPS from "./shaders/forward.ps"
import * as Camera from "./camera/camera"

export
{
	Light,
	Model,
	Instance,
	Camera,
	Texture,
};

/**
 * Geometry type enumeration
 */
export var EGeom = Object.freeze(
{
	"None":0,
	"Vert":1 << 0,
	"Colr":1 << 1,
	"Norm":1 << 2,
	"Tex0":1 << 3,
});

/**
 * Light source type enumeration
 */
export var ELight = Object.freeze(
{
	"Ambient": 0,
	"Directional": 1,
	"Radial": 2,
	"Spot": 3,
});

/**
 * Rendering control flags
 */
export var EFlags = Object.freeze(
{
	"None": 0,

	// The object is hidden
	"Hidden": 1 << 0,

	// The object is filled in wireframe mode
	"Wireframe": 1 << 1,

	// Render the object without testing against the depth buffer
	"NoZTest": 1 << 2,

	// Render the object without effecting the depth buffer
	"NoZWrite": 1 << 3,

	// Set when an object is selected. The meaning of 'selected' is up to the application
	"Selected": 1 << 8,

	// Doesn't contribute to the bounding box on an object.
	"BBoxExclude": 1 << 9,

	// Should not be included when determining the bounds of a scene.
	"SceneBoundsExclude": 1 << 10,

	// Ignored for hit test ray casts
	"HitTestExclude": 1 << 11,
});

/**
 * Create a renderer instance for a canvas element.
 * @param {HTMLCanvasElement} canvas the html canvas element to render on.
 * @returns the WebGL context object.
 */
export function Create(canvas)
{
	let rdr = canvas.getContext("experimental-webgl", { antialias: true, depth: true});
	if (!rdr)
		throw new Error("webgl not available");

	// Set the initial viewport dimensions
	rdr.viewport(0, 0, canvas.width, canvas.height);
	
	// Set the default back colour (r,g,b,a)
	rdr.back_colour = v4.make(0.5, 0.5, 0.5, 1.0);

	// Create stock textures.
	Texture.CreateStockTextures(rdr);

	// Initialise the standard forward rendering shader
	// Component shaders are stored in 'shaders'
	// Compiled shader programs are stored in 'shader_programs
	// User code can do similar to add new shader programs constructed
	// from existing or new shader components.
	rdr.shaders = [];
	rdr.shaders['forward_vs'] = FwdVS.CompileShader(rdr);
	rdr.shaders['forward_ps'] = FwdPS.CompileShader(rdr);
	
	rdr.shader_programs = [];
	rdr.shader_programs['forward'] = function()
	{
		// Compile into a shader 'program'
		let shader = rdr.createProgram();
		rdr.attachShader(shader, rdr.shaders['forward_vs']);
		rdr.attachShader(shader, rdr.shaders['forward_ps']);
		rdr.linkProgram(shader);
		if (!rdr.getProgramParameter(shader, rdr.LINK_STATUS))
			throw new Error('Could not compile WebGL program. \n\n' + rdr.getProgramInfoLog(shader));

		// Read variables from the shaders and save them in 'shader'
		shader.position = rdr.getAttribLocation(shader, "position");
		shader.normal = rdr.getAttribLocation(shader, "normal");
		shader.colour = rdr.getAttribLocation(shader, "colour");
		shader.texcoord = rdr.getAttribLocation(shader, "texcoord");

		shader.o2w = rdr.getUniformLocation(shader, "o2w");
		shader.w2c = rdr.getUniformLocation(shader, "w2c");
		shader.c2s = rdr.getUniformLocation(shader, "c2s");

		shader.tint_colour = rdr.getUniformLocation(shader, "tint_colour");

		shader.camera = {};
		shader.camera.ws_position = rdr.getUniformLocation(shader, "cam_ws_position");
		shader.camera.ws_forward = rdr.getUniformLocation(shader, "cam_ws_forward");

		shader.tex_diffuse = {};
		shader.tex_diffuse.enabled = rdr.getUniformLocation(shader, "has_tex_diffuse");
		shader.tex_diffuse.sampler = rdr.getUniformLocation(shader, "sampler_diffuse");

		shader.light = {};
		shader.light.lighting_type = rdr.getUniformLocation(shader, "lighting_type");
		shader.light.position = rdr.getUniformLocation(shader, "light_position");
		shader.light.direction = rdr.getUniformLocation(shader, "light_direction");
		shader.light.ambient = rdr.getUniformLocation(shader, "light_ambient");
		shader.light.diffuse = rdr.getUniformLocation(shader, "light_diffuse");
		shader.light.specular = rdr.getUniformLocation(shader, "light_specular");
		shader.light.specpwr = rdr.getUniformLocation(shader, "light_specpwr");

		// Return the shader
		return shader;
	}();

	// Return the WebGL context
	return rdr;
}

/**
 * Render a scene
 * @param {WebGL} rdr the WebGL instance created by 'Initialise'
 * @param {[Instance]} instances the instances that make up the scene
 * @param {Camera} camera the view into the scene
 * @param {Light} global_light the global light source
 */
export function Render(rdr, instances, camera, global_light)
{
	// Build a nugget draw list
	let drawlist = BuildDrawList(instances);

	// Clear the back/depth buffer
	rdr.clearColor(rdr.back_colour[0], rdr.back_colour[1], rdr.back_colour[2], rdr.back_colour[3]);
	rdr.clear(rdr.COLOR_BUFFER_BIT | rdr.DEPTH_BUFFER_BIT);

	// Render each nugget
	for (let i = 0; i != drawlist.length; ++i)
	{
		var dle = drawlist[i];
		let nug = dle.nug;
		let inst = dle.inst;
		let model = inst.model;
		let shader = nug.shader;

		// Bind the shader to the gfx card
		rdr.useProgram(shader);

		{// Set render states
			// Z Test
			if ((inst.flags & EFlags.NoZTest) == 0)
				rdr.enable(rdr.DEPTH_TEST);
			else
				rdr.disable(rdr.DEPTH_TEST);
			
			// Z Write
			if ((inst.flags & EFlags.NoZWrite) == 0)
				rdr.depthMask(true);
			else
				rdr.depthMask(false);
		}

		{// Bind shader input streams
			// Bind vertex buffer
			if (model.geom & EGeom.Vert)
			{
				rdr.bindBuffer(rdr.ARRAY_BUFFER, model.vbuffer);
				rdr.vertexAttribPointer(shader.position, model.vbuffer.stride, rdr.FLOAT, false, 0, 0);
				rdr.enableVertexAttribArray(shader.position);
			}
			else if (shader.position != -1)
			{
				rdr.vertexAttrib3fv(shader.colour, [0,0,0]);
				rdr.disableVertexAttribArray(shader.position);
			}

			// Bind normals buffer
			if (model.geom & EGeom.Norm)
			{
				rdr.bindBuffer(rdr.ARRAY_BUFFER, model.nbuffer);
				rdr.vertexAttribPointer(shader.normal, model.nbuffer.stride, rdr.FLOAT, false, 0, 0);
				rdr.enableVertexAttribArray(shader.normal);
			}
			else if (shader.normal != -1)
			{
				rdr.vertexAttrib3fv(shader.normal, [0,0,0]);
				rdr.disableVertexAttribArray(shader.normal);
			}

			// Bind colours buffer
			if (model.geom & EGeom.Colr)
			{
				rdr.bindBuffer(rdr.ARRAY_BUFFER, model.cbuffer);
				rdr.vertexAttribPointer(shader.colour, model.cbuffer.stride, rdr.FLOAT, false, 0, 0);
				rdr.enableVertexAttribArray(shader.colour);
			}
			else if (shader.colour != -1)
			{
				rdr.vertexAttrib3fv(shader.colour, [1,1,1]);
				rdr.disableVertexAttribArray(shader.colour);
			}
			
			// Bind texture coords buffer
			if (model.geom & EGeom.Tex0)
			{
				rdr.bindBuffer(rdr.ARRAY_BUFFER, model.tbuffer);
				rdr.vertexAttribPointer(shader.texcoord, model.tbuffer.stride, rdr.FLOAT, false, 0, 0);
				rdr.enableVertexAttribArray(shader.texcoord);
			}
			else if (shader.texcoord != -1)
			{
				rdr.vertexAttrib2fv(shader.texcoord, [0,0]);
				rdr.disableVertexAttribArray(shader.texcoord);
			}
		}

		{// Bind the diffuse texture
			// Get the texture to bind, or the default texture if not ready yet
			var texture = (nug.tex_diffuse && nug.tex_diffuse.image_loaded) ? nug.tex_diffuse : rdr.stock_textures.white;

			// Set the enable texture flag
			if (shader.tex_diffuse.enabled != -1)
				rdr.uniform1i(shader.tex_diffuse.enabled, nug.tex_diffuse ? +1 : 0);

			// Set the texture unit
			if (shader.tex_diffuse.sampler != -1)
				rdr.uniform1i(shader.tex_diffuse.sampler, 0);

			// Bind the texture
			rdr.bindTexture(rdr.TEXTURE_2D, texture);
			rdr.activeTexture(rdr.TEXTURE0);
		}

		{// Set lighting
			// Use the global light, or one from the nugget
			let light = nug.hasOwnProperty("light") ? nug.light : global_light;
			if (model.geom & EGeom.Norm)
			{
				rdr.uniform1i(shader.light.lighting_type, light.lighting_type);
				rdr.uniform3fv(shader.light.position, light.position.subarray(0,3));
				rdr.uniform3fv(shader.light.direction, light.direction.subarray(0,3));
				rdr.uniform3fv(shader.light.ambient, light.ambient.slice(0,3));
				rdr.uniform3fv(shader.light.diffuse, light.diffuse.slice(0,3));
				rdr.uniform3fv(shader.light.specular, light.specular.slice(0,3));
				rdr.uniform1f(shader.light.specpwr, light.specpwr);
			}
			else if (shader.light.lighting_type != -1)
			{
				rdr.uniform1i(shader.light.lighting_type, 0);
			}
		}

		{// Set tint
			let tint =
				inst.hasOwnProperty("tint") ? inst.tint :
				nug.hasOwnProperty("tint") ? nug.tint :
				[1,1,1,1];
			rdr.uniform4fv(shader.tint_colour, new Float32Array(tint));
		}

		{// Set transforms
			// Set the projection matrix
			let c2s = nug.hasOwnProperty("c2s") ? nug.c2s : camera.c2s;
			rdr.uniformMatrix4fv(shader.c2s, false, c2s);
			
			// Set the camera matrix
			rdr.uniformMatrix4fv(shader.w2c, false, camera.w2c);
			rdr.uniform3fv(shader.camera.ws_position, camera.pos.subarray(0,3));
			rdr.uniform3fv(shader.camera.ws_forward, camera.fwd.subarray(0,3));

			// Set the o2w transform
			rdr.uniformMatrix4fv(shader.o2w, false, inst.o2w);
		}

		{// Render the nugget
			if (model.ibuffer != null)
			{
				// Get the buffer range to render
				let irange = nug.hasOwnProperty("irange") ? nug.irange : {ofs: 0, count: model.ibuffer.count};
				rdr.bindBuffer(rdr.ELEMENT_ARRAY_BUFFER, model.ibuffer);
				rdr.drawElements(nug.topo, irange.count, rdr.UNSIGNED_SHORT, irange.ofs);
			}
			else
			{
				// Get the buffer range to render
				let vrange = nug.hasOwnProperty("vrange") ? nug.vrange : {ofs: 0, count: model.vbuffer.count};
				rdr.drawArrays(nug.topo, vrange.ofs, vrange.count);
			}
		}

		// Clean up
		rdr.bindTexture(rdr.TEXTURE_2D, null);
		rdr.bindBuffer(rdr.ELEMENT_ARRAY_BUFFER, null);
		rdr.bindBuffer(rdr.ARRAY_BUFFER, null);
	}
}

/**
 * Returns an ordered list of nuggets to render
 * @param {[Instance]} instances the instances to build the drawlist from
 * @returns {[Nugget]} an ordered list of nuggets to render
 */
function BuildDrawList(instances)
{
	let drawlist = []
	for (let i = 0; i != instances.length; ++i)
	{
		let inst = instances[i];

		if (!inst.model)
			continue;

		for (let j = 0; j != inst.model.gbuffer.length; ++j)
		{
			let nug = inst.model.gbuffer[j];
			drawlist.push({nug: nug, inst: inst});
		}
	}
	return drawlist;
}

/**
 * Create a demo scene to show everything is working
 */
export function CreateDemoScene(rdr)
{
	let instances = [CreateTestModel(rdr)];

	let camera = Camera.Create(Maths.TauBy8, rdr.AspectRatio);
	m4x4.LookAt(v4.make(4, 4, 10, 1), v4.Origin, v4.YAxis, camera.c2w);

	let light = new Light.Create(Rdr.ELight.Directional, v4.create(), v4.make(-0.5,-0.5,-1.0,0), [0.1,0.1,0.1], [0.5,0.5,0.5], [0.5,0.5,0.5], 100);

	Render(rdr, instances, camera, light);
}

/**
 * Create a test model instance
 * @returns {Instance}
 */
export function CreateTestModel(rdr)
{
	// Use the standard forward render shader
	let shader = rdr.shader_programs.forward;

	// Pyramid
	let model = Rdr.Model.Create(rdr,
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
		{ topo: rdr.TRIANGLES, shader: shader }//, tex_diffuse: rdr.stock_textures.checker1 }
	]);

	let o2w = m4x4.Translation([0, 0, 0, 1]);
	let inst = Rdr.Instance.Create("pyramid", model, o2w);
	return inst;
}