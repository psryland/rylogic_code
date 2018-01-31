import * as Rdr from "../renderer";

/**
 * Create a model.
 * 'nuggets' is an array of
 *    [{
 *      topo: rdr.TRIANGLES,
 *      shader: null,
 *      tex_diffuse: null,                               // optional
 *      tint: [1,1,1,1],                                 // optional
 *      light: Light.Directional([-1,-1,-1], [1,1,0,1]), // optional
 *      irange: {ofs:0, count:indices.length},           // optional
 *      vrange: {ofs:0, count:verts.length}              // optional
 *    },..]
 * @param {WebGL} rdr the WebGL instance returned from rdr.Initialise
 * @param {[{pos:[x,y,z], norm:[x,y,z], col:[r,g,b,a], tex0:[u,v]},...]} verts the array of vertex data for the model
 * @param {[Number]} indices the index data for the model (ushort)
 * @param {[]} nuggets the nugget data for the model
 * @param {const} opts (optional) the usage for the model buffers (default: rdr.STATIC_DRAW)
 * @returns {Model}
 */
export function Create(rdr, verts, indices, nuggets, opts)
{
	let pos = null;
	let norm = null;
	let col = null;
	let tex0 = null;
	let indx = null;
	
	// Fill verts buffer
	if (verts != null)
	{
		// Verts have position data
		if (verts[0].hasOwnProperty("pos"))
		{
			pos = new Float32Array(verts.length * 3);
			for (let i = 0, j = 0; i != verts.length; ++i)
			{
				let p = verts[i].pos;
				pos[j++] = p[0];
				pos[j++] = p[1];
				pos[j++] = p[2];
			}
		}

		// Verts have normals data
		if (verts[0].hasOwnProperty("norm"))
		{
			norm = new Float32Array(verts.length * 3);
			for (let i = 0, j = 0; i != verts.length; ++i)
			{
				let n = verts[i].norm;
				norm[j++] = n[0];
				norm[j++] = n[1];
				norm[j++] = n[2];
			}
		}

		// Verts have colour data
		if (verts[0].hasOwnProperty("col"))
		{
			col = new Float32Array(verts.length * 4);
			for (let i = 0, j = 0; i != verts.length; ++i)
			{
				let c = verts[i].col;
				col[j++] = c[0];
				col[j++] = c[1];
				col[j++] = c[2];
				col[j++] = c[3];
			}
		}

		// Verts have texture coords
		if (verts[0].hasOwnProperty("tex0"))
		{
			tex0 = new Float32Array(verts.length * 2);
			for (let i = 0, j = 0; i != verts.length; ++i)
			{
				let t = verts[i].tex0;
				tex0[j++] = t[0];
				tex0[j++] = t[1];
			}
		}
	}

	// Fill the index buffer
	if (indices != null)
	{
		indx = new Uint16Array(indices);
	}

	return CreateRaw(rdr, pos, norm, col, tex0, indx, nuggets, opts);
}

/**
 * Create a model from separate buffers of vertices, normals, colours, and texture coords
 * @param {Renderer} rdr The renderer main object
 * @param {Float32Array} pos The buffer of vertex positions (stride = 3)
 * @param {Float32Array} norm The buffer of vertex normals (stride = 3)
 * @param {Float32Array} col The buffer of vertex colours (stride = 4)
 * @param {Float32Array} tex0 The buffer of vectex texture coords (stride = 2)
 * @param {Uint16Array} indices The buffer or index data
 * @param {[]} nuggets The buffer of render nuggets
 * @param {} opts (Optional) Optional settings
 * @returns {Model}
 */
export function CreateRaw(rdr, pos, norm, col, tex0, indices, nuggets, opts)
{
	let model =
	{
		geom: Rdr.EGeom.None,
		vbuffer: null,
		nbuffer: null,
		cbuffer: null,
		tbuffer: null,
		ibuffer: null,
		gbuffer: [],
	};

	// Default buffer usage
	let usage = (opts && opts.usage) ? opts.usage : rdr.STATIC_DRAW;

	// Verts
	if (pos != null)
	{
		model.geom = model.geom | Rdr.EGeom.Vert;
		model.vbuffer = rdr.createBuffer();
		model.vbuffer.stride = 3;
		model.vbuffer.count = pos.length / model.vbuffer.stride;
		rdr.bindBuffer(rdr.ARRAY_BUFFER, model.vbuffer);
		rdr.bufferData(rdr.ARRAY_BUFFER, pos, usage);
	}

	// Normals
	if (norm != null)
	{
		model.geom = model.geom | Rdr.EGeom.Norm;
		model.nbuffer = rdr.createBuffer();
		model.nbuffer.stride = 3;
		model.nbuffer.count = norm.length / model.nbuffer.stride;
		rdr.bindBuffer(rdr.ARRAY_BUFFER, model.nbuffer);
		rdr.bufferData(rdr.ARRAY_BUFFER, norm, usage);
	}

	// Colours
	if (col != null)
	{
		model.geom = model.geom | Rdr.EGeom.Colr;
		model.cbuffer = rdr.createBuffer();
		model.cbuffer.stride = 4;
		model.cbuffer.count = col.length / model.cbuffer.stride;
		rdr.bindBuffer(rdr.ARRAY_BUFFER, model.cbuffer);
		rdr.bufferData(rdr.ARRAY_BUFFER, col, usage);
	}

	// Texture Coords
	if (tex0 != null)
	{
		model.geom = model.geom | Rdr.EGeom.Tex0;
		model.tbuffer = rdr.createBuffer();
		model.tbuffer.stride = 2;
		model.tbuffer.count = tex0.length / model.tbuffer.stride;
		rdr.bindBuffer(rdr.ARRAY_BUFFER, model.tbuffer);
		rdr.bufferData(rdr.ARRAY_BUFFER, tex0, usage);
	}

	// Index buffer
	if (indices != null)
	{
		model.ibuffer = rdr.createBuffer();
		model.ibuffer.count = indices.length;
		rdr.bindBuffer(rdr.ELEMENT_ARRAY_BUFFER, model.ibuffer);
		rdr.bufferData(rdr.ELEMENT_ARRAY_BUFFER, indices, usage);
	}

	// Fill the nuggets buffer
	if (nuggets != null)
	{
		if (!nuggets[0].hasOwnProperty("topo"))
			throw new Error("Nuggets must include a topology field");
		if (!nuggets[0].hasOwnProperty("shader"))
			throw new Error("Nuggets must include a shader field");

		model.gbuffer = nuggets;
	}

	return model;
}