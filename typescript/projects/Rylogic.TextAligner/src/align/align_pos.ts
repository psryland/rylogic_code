import { Range } from './range';

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
