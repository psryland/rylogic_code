import * as Math_ from "../../../../rylogic/src/maths/maths";
import * as Range_ from "../../../../rylogic/src/maths/range";
import Range = Math_.Range;

/**
 * A pattern representing an alignment candidate
 */
export class AlignPattern
{
	constructor(patn:string, opt:{ offset?:number, min_width?:number, comment?:string})
	constructor(obj:AlignPattern|any = {} as any)
	{
		let {
			patn = ".",
			offset = 0,
			min_width = 0,
			comment = "",
		} = obj;

		this.pattern = patn;
		this.offset = offset;
		this.minimum_width = min_width;
		this.comment = comment;
	}

	/** The pattern used to match text */
	public pattern: string;

	/** The amount that matching text is offset from the ideal alignment column*/
	public offset: number;

	/** Matched text is padded to be at least this wide */
	public minimum_width: number;

	/** A comment to go with the pattern to remember what it is */
	public comment: string;

	/** Gets the range of characters this pattern should occupy, relative to the aligning column */
	public get position(): Range
	{
		return Range_.create(this.offset, this.offset + this.minimum_width);
	}
}