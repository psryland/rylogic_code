import * as Math_ from "../../maths/maths";
import * as Size_ from "../../maths/size";
import Size = Math_.Size;

/** A 2D transform type */
export interface IMatrix2D
{
	m00: number;
	m01: number;
	m10: number;
	m11: number;
	dx: number;
	dy: number;
}

/**
 * Measure the width and height of 'text'
 * @param gfx The HTML5 canvas 2d context
 * @param text The text to measure
 * @param font The CSS font description
 * @returns The size of the string (width/height)
 */
export function MeasureString(gfx: CanvasRenderingContext2D, text: string, font: string): Size
{
	gfx.save();
	gfx.font = font;

	// Measure width using the 2D canvas API
	let width = gfx.measureText(text).width;

	// Get the font height
	let height = FontHeight(gfx, font);

	gfx.restore();

	// Return the text dimensions
	return Size_.create(width, height);
}

/**
 * Measure the height of a font
 * @param gfx The HTML5 canvas 2d context
 * @param font The CSS font description
 */
export function FontHeight(gfx: CanvasRenderingContext2D, font: string): number
{
	// Get the font height
	let height = font_height_cache[font] || (function()
	{
		let height = 0;

		// Create an off-screen canvas
		let cv = <HTMLCanvasElement>gfx.canvas.cloneNode(false);
		let ctx = <CanvasRenderingContext2D>cv.getContext("2d");

		// Measure the width of 'M' and resize the canvas
		ctx.font = font;
		cv.width = ctx.measureText("M").width;
		cv.height = cv.width * 2;
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
				let i = y * cv.height, iend = i + cv.width;
				for (; i != iend && pixels[i] == 0; ++i) { }
				if (i != iend) height = y;
			}
		}
		return font_height_cache[font] = height;
	}());
	return height;
}
var font_height_cache: { [_: string]: number } = {};
