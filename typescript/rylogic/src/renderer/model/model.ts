import * as Math_ from "../../maths/maths";
import * as Rdr_ from "../renderer";
import M4x4 = Math_.M4x4;
import Vec4 = Math_.Vec4;
import Vec2 = Math_.Vec2;
import { IShaderProgram } from "../shaders/shader";

/** Model vertex type */
export interface IVertex
{
	pos: Vec4;
	norm?: Vec4;
	col?: Vec4;
	tex0?: Vec2;
}

/** Model nugget type */
export interface INugget
{
	/** The geometry topology */
	topo: Rdr_.EPrim;

	/** The shader program to use to render this nugget */
	shader: IShaderProgram;

	/** The diffuse texture to use for this nugget */
	tex_diffuse?: Rdr_.Texture.ITexture;

	/** Per-nugget colour tint */
	tint?: Vec4;

	/** Per-nugget light source */
	light?: Rdr_.Light.ILight;

	/** Per-nugget projection transform */
	c2s?: M4x4;

	/** The index range in the model's index buffer for this nugget */
	irange?: { ofs: number, count: number };

	/** The vertex range in the model's vertex buffers for this nugget */
	vrange?: { ofs: number, count: number };
}

/** Renderable model */
export interface IModel
{
	/** The geometry data components that are valid in this model */
	geom: Rdr_.EGeom,

	/** The vertex positions buffer */
	vbuffer: IBuffer | null;

	/** Per-vertex normals buffer */
	nbuffer: IBuffer | null,

	/** Per-vertex colour buffer */
	cbuffer: IBuffer | null,

	/** Diffuse texture coordinates buffer */
	tbuffer: IBuffer | null,

	/** Index buffer */
	ibuffer: IBuffer | null,

	/** Render nuggets buffer */
	gbuffer: INugget[],
}

/** Model creation options */
export interface IModelOptions
{
	/** Model usage (e.g. gl.STATIC_DRAW) */
	usage: number;
}

/** Extend WebGLBuffer */
export interface IBuffer
{
	/** The WebGL buffer */
	data: WebGLBuffer;

	/** The number of units per element */
	stride: number;

	/** The number of elements in the buffer */
	count: number;
}

/**
 * Create a model from vertex data
 * @param rdr The main renderer instance
 * @param verts The array of vertex data for the model
 * @param indices The index data for the model (ushort)
 * @param nuggets The nugget data for the model
 * @param opts Model specific options 
 */
export function Create(rdr: Rdr_.Renderer, verts: IVertex[], indices: number[], nuggets: INugget[], opts?: IModelOptions): IModel
{
	let pos = null;
	let norm = null;
	let col = null;
	let tex0 = null;
	let indx = null;
	
	// Verts have position data
	pos = new Float32Array(verts.length * 3);
	for (let i = 0, j = 0; i != verts.length; ++i)
	{
		let p = verts[i].pos;
		pos[j++] = p[0];
		pos[j++] = p[1];
		pos[j++] = p[2];
	}

	// Verts have normals data
	if (verts[0].norm)
	{
		norm = new Float32Array(verts.length * 3);
		for (let i = 0, j = 0; i != verts.length; ++i)
		{
			let n = <Vec4>verts[i].norm;
			norm[j++] = n[0];
			norm[j++] = n[1];
			norm[j++] = n[2];
		}
	}

	// Verts have colour data
	if (verts[0].col)
	{
		col = new Float32Array(verts.length * 4);
		for (let i = 0, j = 0; i != verts.length; ++i)
		{
			let c = <Vec4>verts[i].col;
			col[j++] = c[0];
			col[j++] = c[1];
			col[j++] = c[2];
			col[j++] = c[3];
		}
	}

	// Verts have diffuse texture coords
	if (verts[0].tex0)
	{
		tex0 = new Float32Array(verts.length * 2);
		for (let i = 0, j = 0; i != verts.length; ++i)
		{
			let t = <Vec2>verts[i].tex0;
			tex0[j++] = t[0];
			tex0[j++] = t[1];
		}
	}

	// Fill the index buffer
	if (indices != null)
	{
		indx = new Uint16Array(indices);
	}

	// Create the model with using the raw data
	return CreateRaw(rdr, pos, norm, col, tex0, indx, nuggets, opts);
}

/**
 * Create a model from separate buffers of vertices, normals, colours, and texture coords
 * @param rdr The main renderer instance
 * @param pos The buffer of vertex positions (stride = 3)
 * @param norm The buffer of vertex normals (stride = 3)
 * @param col The buffer of vertex colours (stride = 4)
 * @param tex0 The buffer of vertex texture coords (stride = 2)
 * @param indices The buffer or index data
 * @param nuggets The buffer of render nuggets
 * @param opts Model creation options
 */
export function CreateRaw(rdr: Rdr_.Renderer, pos: Float32Array | null, norm: Float32Array | null, col: Float32Array | null, tex0: Float32Array | null, indices: Uint16Array | null, nuggets: INugget[], opts?: IModelOptions): IModel
{
	let gl = rdr.webgl;
	let model:IModel =
		{
			geom: Rdr_.EGeom.None,
			vbuffer: null,
			nbuffer: null,
			cbuffer: null,
			tbuffer: null,
			ibuffer: null,
			gbuffer: [],
		};

	// Default buffer usage
	let usage = (opts && opts.usage) ? opts.usage : gl.STATIC_DRAW;

	// Verts
	if (pos != null)
	{
		model.geom = model.geom | Rdr_.EGeom.Vert;
		model.vbuffer = { data: <WebGLBuffer>gl.createBuffer(), stride: 3, count: pos.length / 3 };
		gl.bindBuffer(gl.ARRAY_BUFFER, model.vbuffer.data);
		gl.bufferData(gl.ARRAY_BUFFER, pos, usage);
	}

	// Normals
	if (norm != null)
	{
		model.geom = model.geom | Rdr_.EGeom.Norm;
		model.nbuffer = { data: <WebGLBuffer>gl.createBuffer(), stride: 3, count: norm.length / 3 };
		gl.bindBuffer(gl.ARRAY_BUFFER, model.nbuffer.data);
		gl.bufferData(gl.ARRAY_BUFFER, norm, usage);
	}

	// Colours
	if (col != null)
	{
		model.geom = model.geom | Rdr_.EGeom.Colr;
		model.cbuffer = { data: <WebGLBuffer>gl.createBuffer(), stride: 4, count: col.length / 4 };
		gl.bindBuffer(gl.ARRAY_BUFFER, model.cbuffer.data);
		gl.bufferData(gl.ARRAY_BUFFER, col, usage);
	}

	// Texture Coords
	if (tex0 != null)
	{
		model.geom = model.geom | Rdr_.EGeom.Tex0;
		model.tbuffer = { data: <WebGLBuffer>gl.createBuffer(), stride: 2, count: tex0.length / 2 };
		gl.bindBuffer(gl.ARRAY_BUFFER, model.tbuffer.data);
		gl.bufferData(gl.ARRAY_BUFFER, tex0, usage);
	}

	// Index buffer
	if (indices != null)
	{
		model.ibuffer = { data: <WebGLBuffer>gl.createBuffer(), stride: 1, count: indices.length };
		gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, model.ibuffer.data);
		gl.bufferData(gl.ELEMENT_ARRAY_BUFFER, indices, usage);
	}

	// Fill the nuggets buffer
	if (nuggets != null)
	{
		if (!nuggets[0].hasOwnProperty("topo"))
			throw new Error("Nuggets must include a topology field");
		if (!nuggets[0].hasOwnProperty("shader"))
			throw new Error("Nuggets must include a shader field");

		// Clone the nuggets buffer
		model.gbuffer = nuggets.slice();
	}

	return model;
}