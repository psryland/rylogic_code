import * as Rdr_ from "../renderer";
import * as Math_ from "../../maths/maths";

/** A texture */
export interface ITexture
{
	/** The GL representation of the texture */
	tex: WebGLTexture;

	/** A flag to indicate the texture is ready for use */
	loaded: boolean;

	/** The source data */
	image?: HTMLImageElement | ArrayBufferView | null;
}

/** Texture loading options */
export interface ITextureOptions
{
	min_filter?: number,
	mag_filter?: number,
	wrap_s?: number,
	wrap_t?: number,
}

/**
 * Create a texture from raw image data
 * @param rdr The main renderer instance
 * @param w The width of the texture (Must be a power of 2)
 * @param h The height of the texture (Must be a power of 2)
 * @param data The pixel colour data for the texture
 * @param opts Texture options
 */
export function CreateFromData(rdr: Rdr_.Renderer, w: number, h: number, data: ArrayBufferView, opts?: ITextureOptions): ITexture
{
	// Validation
	if ((data.byteLength & 3) != 0)
		throw new Error("Data length must be a multiple of 4 bytes");
	if (!Math_.Bits.IsPowerOfTwo(w) || !Math_.Bits.IsPowerOfTwo(h))
		throw new Error("Texture dimensions must be powers of two");

	let gl = rdr.webgl;

	let tex = <WebGLTexture>gl.createTexture();
	gl.bindTexture(gl.TEXTURE_2D, tex);

	// Assign the pixel data
	gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, w, h, 0, gl.RGBA, gl.UNSIGNED_BYTE, data);

	// Set filtering/wrap mode
	gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, (opts && opts.min_filter) ? opts.min_filter : gl.LINEAR);
	gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, (opts && opts.mag_filter) ? opts.mag_filter : gl.LINEAR);
	gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S,     (opts && opts.wrap_s)     ? opts.wrap_s     : gl.CLAMP_TO_EDGE);
	gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T,     (opts && opts.wrap_t)     ? opts.wrap_t     : gl.CLAMP_TO_EDGE);

	// Generate mip maps
	gl.generateMipmap(gl.TEXTURE_2D);

	gl.bindTexture(gl.TEXTURE_2D, null);
	return { tex: tex, loaded: true, image: data };
}

/**
* Create a texture from a file.
* @param rdr The main renderer instance
* @param filepath The filepath to an image to load into the texture
* @param opts Texture parameters
*/
export function CreateFromFile(rdr: Rdr_.Renderer, filepath: string, opts ?: ITextureOptions): ITexture
{
	let gl = rdr.webgl;
	let tex = <WebGLTexture>gl.createTexture();

	// Use an HTMLImageElement to load the image
	let image = new Image();
	image.onload = function()
	{
		gl.bindTexture(gl.TEXTURE_2D, tex);

		// Assign the pixel data
		gl.pixelStorei(gl.UNPACK_FLIP_Y_WEBGL, true);
		gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, image);

		// Set filtering/wrap mode
		gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, (opts && opts.min_filter) ? opts.min_filter : gl.LINEAR);
		gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, (opts && opts.mag_filter) ? opts.mag_filter : gl.LINEAR);
		gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S,     (opts && opts.wrap_s) ?     opts.wrap_s :     gl.CLAMP_TO_EDGE);
		gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T,     (opts && opts.wrap_t) ?     opts.wrap_t :     gl.CLAMP_TO_EDGE);

		// Generate mip maps
		gl.generateMipmap(gl.TEXTURE_2D);
		gl.bindTexture(gl.TEXTURE_2D, null);
		texture.loaded = true;
	}
	image.src = filepath;

	let texture = { tex: tex, loaded: false, image: image };
	return texture;
}


/**
 * Create stock textures.
 * Used while textures are loading and to bind to the sampler when no texture is available.
 * @param rdr The main renderer instance
 */
export function CreateStockTextures(rdr: Rdr_.Renderer): { [_: string]: ITexture }
{
	let gl = rdr.webgl;
	let stock_textures: { [_: string]: ITexture } = {};

	{// White
		let opts = { min_filter: gl.NEAREST, mag_filter: gl.NEAREST, wrap_s: gl.CLAMP_TO_EDGE, wrap_t: gl.CLAMP_TO_EDGE };
		stock_textures.white = CreateFromData(rdr, 1, 1, new Uint8Array([255, 255, 255, 255]), opts);
	}
	{// Checker1
		const dim = 8;
		let i = 0, j = 0;
		let data = new Uint8Array(dim*dim*4);
		for (; i !=  8;) data[i++] = 0xFF;
		for (; i != 16;) data[i++] = 0x00;
		for (; i != data.length;)
		{
			data[i++] = data[j++];
			if ((i % 64) == 0) j -= 8;
		}

		let opts = { wrap_s: gl.REPEAT, wrap_t: gl.REPEAT };
		stock_textures.checker1 = CreateFromData(rdr, dim, dim, data, opts);
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

		let opts = { wrap_s: gl.REPEAT, wrap_t: gl.REPEAT };
		stock_textures.checker3 = CreateFromData(rdr, dim, dim, data, opts);
	}

	return stock_textures;
}
