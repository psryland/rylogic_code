import * as Rdr from "../renderer";

/**
 * Create a texture from a file.
 * @param {WebGL} rdr is the WebGL instance
 * @param {string} filepath is the filepath to an image to load into the texture
 * @param {} opts is an optional parameters structure:
 * {
 *    mag_filter: gl.LINEAR,
 *    min_filter: gl.LINEAR,
 *    wrap_t: gl.CLAMP_TO_EDGE,
 *    wrap_s: gl.CLAMP_TO_EDGE,
 * }
 * @returns {} the created texture
 */
export function Create(rdr, filepath, opts)
{
	let tex = rdr.createTexture();
	tex.image = new Image();
	tex.image.onload = function()
	{
		rdr.bindTexture(rdr.TEXTURE_2D, tex);
		rdr.pixelStorei(rdr.UNPACK_FLIP_Y_WEBGL, true);
		rdr.texImage2D(rdr.TEXTURE_2D, 0, rdr.RGBA, rdr.RGBA, rdr.UNSIGNED_BYTE, tex.image);
		rdr.texParameteri(rdr.TEXTURE_2D, rdr.TEXTURE_MAG_FILTER, (opts && opts.mag_filter) ? opts.mag_filter : rdr.LINEAR);
		rdr.texParameteri(rdr.TEXTURE_2D, rdr.TEXTURE_MIN_FILTER, (opts && opts.min_filter) ? opts.min_filter : rdr.LINEAR);
		rdr.texParameteri(rdr.TEXTURE_2D, rdr.TEXTURE_WRAP_T, (opts && opts.wrap_t) ? opts.wrap_t : rdr.CLAMP_TO_EDGE);
		rdr.texParameteri(rdr.TEXTURE_2D, rdr.TEXTURE_WRAP_S, (opts && opts.wrap_s) ? opts.wrap_s : rdr.CLAMP_TO_EDGE);
		rdr.generateMipmap(rdr.TEXTURE_2D);
		rdr.bindTexture(rdr.TEXTURE_2D, null);
		tex.image_loaded = true;
	}
	tex.image.src = filepath;
	tex.image_loaded = false;
	return tex;
}

/**
 * Create stock textures.
 * Used while textures are loading and to bind to the sampler when no texture is available.
 */
export function CreateStockTextures(rdr)
{
	rdr.stock_textures = {};

	{// White
		rdr.stock_textures.white = rdr.createTexture();
		rdr.bindTexture(rdr.TEXTURE_2D, rdr.stock_textures.white);
		rdr.texImage2D(rdr.TEXTURE_2D, 0, rdr.RGBA, 1, 1, 0, rdr.RGBA, rdr.UNSIGNED_BYTE, new Uint8Array([255,255,255,255]));
		rdr.texParameteri(rdr.TEXTURE_2D, rdr.TEXTURE_MAG_FILTER, rdr.NEAREST);
		rdr.texParameteri(rdr.TEXTURE_2D, rdr.TEXTURE_MIN_FILTER, rdr.NEAREST);
		rdr.texParameteri(rdr.TEXTURE_2D, rdr.TEXTURE_WRAP_T, rdr.CLAMP_TO_EDGE);
		rdr.texParameteri(rdr.TEXTURE_2D, rdr.TEXTURE_WRAP_S, rdr.CLAMP_TO_EDGE);
		rdr.generateMipmap(rdr.TEXTURE_2D);
		rdr.stock_textures.white.image_loaded = true;
	}
	{// Checker1
		const dim = 8;
		const c = 0xFF;
		let i = 0, j = 0;
		let data = new Uint8Array(dim*dim*4);
		for (; i !=  8;) data[i++] = 0xFF;
		for (; i != 16;) data[i++] = 0x00;
		for (; i != data.length;)
		{
			data[i++] = data[j++];
			if ((i % 64) == 0) j -= 8;
		}

		rdr.stock_textures.checker1 = rdr.createTexture();
		rdr.bindTexture(rdr.TEXTURE_2D, rdr.stock_textures.checker1);
		rdr.texImage2D(rdr.TEXTURE_2D, 0, rdr.RGBA, dim, dim, 0, rdr.RGBA, rdr.UNSIGNED_BYTE, data);
		rdr.texParameteri(rdr.TEXTURE_2D, rdr.TEXTURE_MAG_FILTER, rdr.LINEAR);
		rdr.texParameteri(rdr.TEXTURE_2D, rdr.TEXTURE_MIN_FILTER, rdr.LINEAR);
		rdr.texParameteri(rdr.TEXTURE_2D, rdr.TEXTURE_WRAP_T, rdr.REPEAT);
		rdr.texParameteri(rdr.TEXTURE_2D, rdr.TEXTURE_WRAP_S, rdr.REPEAT);
		rdr.generateMipmap(rdr.TEXTURE_2D);
		rdr.stock_textures.checker1.image_loaded = true;
	}
	{// Checker3
		const dim = 8;
		let i = 0, j = 0;
		var data = new Uint8Array(dim*dim*4);
		for (; i !=  9;) data[i++] = 0xFF;
		for (; i != 16;) data[i++] = 0xEE;
		for (; i != data.length;)
		{
			data[i++] = data[j++];
			if ((i % 64) == 0) j -= 8;
		}

		rdr.stock_textures.checker3 = rdr.createTexture();
		rdr.bindTexture(rdr.TEXTURE_2D, rdr.stock_textures.checker3);
		rdr.texImage2D(rdr.TEXTURE_2D, 0, rdr.RGBA, dim, dim, 0, rdr.RGBA, rdr.UNSIGNED_BYTE, data);
		rdr.texParameteri(rdr.TEXTURE_2D, rdr.TEXTURE_MAG_FILTER, rdr.LINEAR);
		rdr.texParameteri(rdr.TEXTURE_2D, rdr.TEXTURE_MIN_FILTER, rdr.LINEAR);
		rdr.texParameteri(rdr.TEXTURE_2D, rdr.TEXTURE_WRAP_T, rdr.REPEAT);
		rdr.texParameteri(rdr.TEXTURE_2D, rdr.TEXTURE_WRAP_S, rdr.REPEAT);
		rdr.generateMipmap(rdr.TEXTURE_2D);
		rdr.stock_textures.checker3.image_loaded = true;
	}
	rdr.bindTexture(rdr.TEXTURE_2D, null);
}
