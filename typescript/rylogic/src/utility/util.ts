import * as Math_ from "../maths/maths"
import * as Vec4_ from "../maths/v4"
import * as DF_ from "./date_format"
import Vec4 = Math_.Vec4;

export var DateFormat = DF_.DateFormat;

/**
 * Invoke a function on the next message
 */
export function BeginInvoke(func:()=>void): void
{
	window.setTimeout(func, 0);
}

/**
 * Copy a range of objects from one array to another
 * @param dst The array to copy objects to
 * @param dst_index The first index to start copying values to
 * @param src The array to copy objects from
 * @param src_index The first index to start copying values from
 * @param count The number to copy
 */
export function MemCopy(dst: any[], dst_index: number, src: any[], src_index: number, count: number): void
{
	for (; count-- != 0;)
		dst[dst_index++] = src[src_index++];
}

/**
 * Variadic function that copies objects into 'dst'
 * Use: CopyTo(my_array, 0, thing1, thing2, thing3, ...)
 * @param dst The array to copy items into
 * @param dst_index The first index to start copying to
 * @returns Returns 'dst'
 */
export function CopyTo(dst: any[], dst_index: number, ...items: any[]): any[]
{
	for (let i = 0; i != items.length; ++i)
		dst[dst_index++] = items[i];
	return dst;
}

/**
 * Return a string with 'chars' removed from the left hand side
 * @param str The string to trim
 * @param chars An array of single-character strings to remove from 'str'
 */
export function TrimLeft(str: string, chars: string): string
{
	for (var s = 0; s != str.length && chars.indexOf(str[s]) != -1; ++s) { }
	return str.slice(s);
}

/**
 * Return a string with 'chars' removed from the right hand side
 * @param str The string to trim
 * @param An array of single-character strings to remove from 'str'
 */
export function TrimRight(str: string, chars: string): string
{
	for (var e = str.length; e-- != 0 && chars.indexOf(str[e]) != -1;) { }
	return str.slice(0, e + 1);
}

/**
 * Return a string with 'chars' removed from both ends
 * @param str The string to trim
 * @param chars An array of single-character strings to remove from 'str'
 */
export function Trim(str:string, chars: string): string
{
	for (var s = 0; s != str.length && chars.indexOf(str[s]) != -1; ++s){}
	for (var e = str.length; e-- != 0 && chars.indexOf(str[e]) != -1;){}
	return str.slice(s, e+1);
}

/**
 * Convert a CSS colour to an unsigned int
 * @param colour A CSS colour, e.g. #RRGGBB
 * @returns The colour as a uint (alpha = 0xff)
 */
export function ColourToUint(colour: string): number
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
 * @param colour A CSS colour, e.g. #RRGGBB
 * @returns The colour as an array of floats
 */
export function ColourToV4(colour:string) :Vec4
{
	if (colour[0] == '#')
	{
		if (colour.length == 9) // #aarrggbb
		{
			let a = parseInt(colour.substr(1,2), 16) / 0xff;
			let r = parseInt(colour.substr(3,2), 16) / 0xff;
			let g = parseInt(colour.substr(5,2), 16) / 0xff;
			let b = parseInt(colour.substr(7,2), 16) / 0xff;
			return Vec4_.create(r, g, b, a);
		}
		if (colour.length == 7) // #rrggbb
		{
			let r = parseInt(colour.substr(1,2), 16) / 0xff;
			let g = parseInt(colour.substr(3,2), 16) / 0xff;
			let b = parseInt(colour.substr(5,2), 16) / 0xff;
			return Vec4_.create(r, g, b, 1);
		}
		if (colour.length == 4) // #rgb
		{
			let g = parseInt(colour[1], 16) / 0x0f;
			let r = parseInt(colour[2], 16) / 0x0f;
			let b = parseInt(colour[3], 16) / 0x0f;
			return Vec4_.create(r, g, b, 1);
		}
	}
	switch (colour.toLowerCase())
	{
		case 'black': return Vec4_.create(0,0,0,1);
		case 'white': return Vec4_.create(1,1,1,1);
		case 'red': return Vec4_.create(1,0,0,1);
		case 'green': return Vec4_.create(0,1,0,1);
		case 'blue': return Vec4_.create(0,0,1,1);
	}
	throw new Error("Unsupported colour format");
}

/**
 * Convert a AARRGGBB colour to a Vec4 colour
 * @param colour The colour as a 32bit number.
 * @returns The colour as an array of floats
 */
export function ColourUintToV4(colour: number): Vec4
{
	return Vec4_.create(
		((colour & 0xFF000000) >> 24) / 0xff,
		((colour & 0x00FF0000) >> 16) / 0xff,
		((colour & 0x0000FF00) >>  8) / 0xff,
		((colour & 0x000000FF) >>  0) / 0xff);
}


//"#if UNITTEST"
//import * as UT from "./unittests";
//{
//	{// Boundary cases
//		let arr = [];
//		let d = Partition(arr, function(x){ return x%2 == 0; }, false);
//		UT.Assert(d == 0);
//		UT.Assert(UT.EqlN(arr, []))

//		arr = [1];
//		d = Partition(arr, function(x){ return x%2 == 0; }, false);
//		UT.Assert(d == 0)
//		UT.Assert(UT.EqlN(arr, [1]))

//		arr = [2];
//		d = Partition(arr, function(x){ return x%2 == 0; }, false);
//		UT.Assert(d == 1)
//		UT.Assert(UT.EqlN(arr, [2]))

//		arr = [1,3,5,7];
//		d = Partition(arr, function(x){ return x%2 == 0; }, false);
//		UT.Assert(d == 0)
//		UT.Assert(UT.EqlN(arr, [1,3,5,7]))

//		arr = [2,4,6,8];
//		d = Partition(arr, function(x){ return x%2 == 0; }, false);
//		UT.Assert(d == 4)
//		UT.Assert(UT.EqlN(arr, [2,4,6,8]))
//	}
//	{// Unstable partition
//		let arr = [1,2,3,4,0,9,8,7,6,5,5,4,4];
//		let d = Partition(arr, function(x){ return x%2 == 0; }, false);
//		UT.Assert(d == 7)
//		UT.Assert(UT.EqlN(arr, [2,4,0,8,6,4,4,7,3,5,5,9,1]))
//	}
//	{// Stable partition
//		let arr = [1,2,3,4,0,9,8,7,6,5,5,4,4];
//		let d= Partition(arr, function(x){ return x%2 == 0; }, true);
//		UT.Assert(d == 7)
//		UT.Assert(UT.EqlN(arr, [2,4,0,8,6,4,4,1,3,9,7,5,5]))
//	}
//}
//"#endif"
