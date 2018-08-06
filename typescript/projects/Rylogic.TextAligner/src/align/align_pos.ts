import * as Math_ from "../../../../rylogic/src/maths/maths";
import Range = Math_.Range;

/** */
export class AlignPos
{
	constructor(column: number, span:Range)
	{
		this.column = column;
		this.span = span;
	}

	/** The column index to align to */
	public readonly column: number;

	/** The range of characters around the align column */
	public readonly span :Range;

	/** ToString */
	public toString(): string
	{
		return `Col ${this.column} ${this.span}`;
	}
}
