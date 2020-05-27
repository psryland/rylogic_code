import { Range } from './range';

/** A pattern representing an alignment candidate */
export class AlignPattern
{
	// WARNING: this type is loaded from JSON which doesn't call the constructor.
	// That means functions and properties will be 'undefined'
	constructor(pattern:RegExp, opt:{ offset?:number, min_width?:number, comment?:string })
	{
		let
		{
			offset = 0,
			min_width = 0,
			comment = "",
		} = opt;

		this.pattern = pattern ? pattern : /./g;
		this.offset = offset;
		this.minimum_width = min_width;
		this.comment = comment;
	}

	/** The pattern used to match text */
	public readonly pattern: RegExp;

	/** The amount that matching text is offset from the ideal alignment column*/
	public readonly offset: number;

	/** Matched text is padded to be at least this wide */
	public readonly minimum_width: number;

	/** A comment to go with the pattern to remember what it is */
	public readonly comment: string;

	/** Returns the range of characters this pattern should occupy, relative to the aligning column */
	get position() :Range
	{
		return new Range(this.offset, this.offset + this.minimum_width);
	}
}
