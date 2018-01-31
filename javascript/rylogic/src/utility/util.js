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
	if (colour.length == 7 && colour[0] == '#')
	{
		return (0xFF000000 | parseInt(colour.substr(1), 16));
	}
	if (colour.length == 4 && colour[0] == '#')
	{
		let g = parseInt(colour[1], 16) * 0xff / 0x0f;
		let r = parseInt(colour[2], 16) * 0xff / 0x0f;
		let b = parseInt(colour[3], 16) * 0xff / 0x0f;
		return 0xFF000000 | (r << 16) | (g << 8) | (b);
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
	if (colour.length == 7 && colour[0] == '#')
	{
		let r = parseInt(colour.substr(1,2), 16) / 0xff;
		let g = parseInt(colour.substr(3,2), 16) / 0xff;
		let b = parseInt(colour.substr(5,2), 16) / 0xff;
		return v4.make(1, r, g, b);
	}
	if (colour.length == 4 && colour[0] == '#')
	{
		let g = parseInt(colour[1], 16) / 0x0f;
		let r = parseInt(colour[2], 16) / 0x0f;
		let b = parseInt(colour[3], 16) / 0x0f;
		return v4.make(1, r, g, b);
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