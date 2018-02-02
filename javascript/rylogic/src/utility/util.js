/**
 * Copy a range of objects from one array to another
 * @param {Array} dst 
 * @param {Number} dst_index 
 * @param {Array} src 
 * @param {Number} src_index 
 * @param {Number} count 
 */
export function MemCopy(dst, dst_index, src, src_index, count)
{
	for (;count-- != 0;)
		dst[dst_index++] = src[src_index++];
}

/**
 * Variadic function that copies objects into 'dst'
 * @param {Array} dst 
 * @param {Number} dst_index 
 * @returns {Array} Returns 'dst'
 */
export function CopyTo(dst, dst_index)
{
	for (let i = 2; i != arguments.length; ++i)
		dst[dst_index++] = arguments[i];
	return dst;
}

/**
 * Return a string with 'chars' removed from the left hand side
 * @param {string} str The string to trim
 * @param {[]} chars An array of characters to remove from 'str'
 * @returns {string}
 */
export function TrimLeft(str, chars)
{
	for (var s = 0; s != str.length && chars.indexOf(str[s]) != -1; ++s){}
	return str.slice(s);
}

/**
 * Return a string with 'chars' removed from the right hand side
 * @param {string} str The string to trim
 * @param {[]} chars An array of characters to remove from 'str'
 * @returns {string}
 */
export function TrimRight(str, chars)
{
	for (var e = str.length; e-- != 0 && chars.indexOf(str[e]) != -1;){}
	return str.slice(0, e+1);
}

/**
 * Return a string with 'chars' removed from both ends
 * @param {string} str The string to trim
 * @param {[]} chars An array of characters to remove from 'str'
 * @returns {string}
 */
export function Trim(str, chars)
{
	for (var s = 0; s != str.length && chars.indexOf(str[s]) != -1; ++s){}
	for (var e = str.length; e-- != 0 && chars.indexOf(str[e]) != -1;){}
	return str.slice(s, e+1);
}

/**
 * Convert a CSS colour to an unsigned int
 * @param {string} colour A CSS colour, e.g. #RRGGBB
 * @returns {Number} The colour as a uint (alpha = 0xff)
 */
export function ColourToUint(colour)
{
	if (colour[0] == '#')
	{
		if (colour.length == 9) // #aarrggbb
		{
			return parseInt(colour.substr(1), 16);
		}
		if (colour.length == 7) // #rrggbb
		{
			return (0xFF000000 | parseInt(colour.substr(1), 16));
		}
		if (colour.length == 4) // #rgb
		{
			let g = parseInt(colour[1], 16) * 0xff / 0x0f;
			let r = parseInt(colour[2], 16) * 0xff / 0x0f;
			let b = parseInt(colour[3], 16) * 0xff / 0x0f;
			return 0xFF000000 | (r << 16) | (g << 8) | (b);
		}
	}
	switch (colour.toLowerCase())
	{
		case 'black': return 0xFF000000;
		case 'white': return 0xFFFFFFFF;
		case 'red': return 0xFFFF0000;
		case 'green': return 0xFF00FF00;
		case 'blue': return 0xFF0000FF;
	}
	throw new Error("Unsupported colour format");
}

/**
 * Convert a CSS colour to an array of floats [r,g,b,a]
 * @param {string} colour A CSS colour, e.g. #RRGGBB
 * @returns {v4} The colour as an array of floats
 */
export function ColourToV4(colour)
{
	if (colour[0] == '#')
	{
		if (colour.length == 9) // #aarrggbb
		{
			let a = parseInt(colour.substr(1,2), 16) / 0xff;
			let r = parseInt(colour.substr(3,2), 16) / 0xff;
			let g = parseInt(colour.substr(5,2), 16) / 0xff;
			let b = parseInt(colour.substr(7,2), 16) / 0xff;
			return v4.make(r, g, b, a);
		}
		if (colour.length == 7) // #rrggbb
		{
			let r = parseInt(colour.substr(1,2), 16) / 0xff;
			let g = parseInt(colour.substr(3,2), 16) / 0xff;
			let b = parseInt(colour.substr(5,2), 16) / 0xff;
			return v4.make(r, g, b, 1);
		}
		if (colour.length == 4) // #rgb
		{
			let g = parseInt(colour[1], 16) / 0x0f;
			let r = parseInt(colour[2], 16) / 0x0f;
			let b = parseInt(colour[3], 16) / 0x0f;
			return v4.make(r, g, b, 1);
		}
	}
	switch (colour.toLowerCase())
	{
		case 'black': return v4.make(0,0,0,1);
		case 'white': return v4.make(1,1,1,1);
		case 'red': return v4.make(1,0,0,1);
		case 'green': return v4.make(0,1,0,1);
		case 'blue': return v4.make(0,0,1,1);
	}
	throw new Error("Unsupported colour format");
}


/**
 * Measure the width and height of 'text'
 * @param {CanvasRenderingContext2D} gfx
 * @param {string} text The text to measure
 * @param {Font} font The font to use to render the text
 * @returns {{width, height}} 
 */
export function MeasureString(gfx, text, font)
{
	// Measure width using the 2D canvas API
	let width = gfx.measureText(text, font).width;

	// Get the font height
	let height = font_height_cache[font] || (function()
	{
		let height = 0;

		// Create an off-screen canvas
		let cv = gfx.canvas.cloneNode(false);
		let ctx = cv.getContext("2d");

		// Measure the width of 'M' and resize the canvas
		ctx.font = font;
		cv.width = ctx.measureText("M", font).width;
		cv.height = cv.width*2;
		if (cv.width != 0)
		{
			// Draw 'M'and 'p' onto the canvas so we can measure the height.
			// Changing the width/height means the properties need setting again.
			ctx.fillRect(0, 0, cv.width, cv.height);
			ctx.imageSmoothingEnabled = false;
			ctx.textBaseline = 'top';
			ctx.fillStyle = 'white';
			ctx.font = font;
			ctx.fillText("M", 0, 0);
			ctx.fillText("p", 0, 0);
			
			// Scan for white pixels
			// Record the last row to have any non-black pixels
			let pixels = ctx.getImageData(0, 0, cv.width, cv.height).data;
			for (let y = 0; y != cv.height; ++y)
			{
				let i = y*cv.height, iend = i + cv.width;
				for (; i != iend && pixels[i] == 0; ++i) {}
				if (i != iend) height = y;
			}
		}
		return font_height_cache[font] = height;
	}());
	return {width: width, height: height};
}
var font_height_cache = {}

/**
 * A C#-isk event type
 */
export class MulticastDelegate
{
	constructor()
	{
		this.m_handlers = [];
	}
	sub(handler)
	{
		let idx = this.m_handlers.indexOf(handler);
		this.m_handlers.push(handler);
	}
	unsub(handler)
	{
		let idx = this.m_handlers.indexOf(handler);
		if (idx != -1) this.m_handlers.splice(idx,1);
	}
	invoke(sender, args)
	{
		for (let i = 0; i != this.m_handlers.length; ++i)
			this.m_handlers[i](sender, args)
	}
}