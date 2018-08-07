import {AlignPattern} from './align_pattern';

/** Represents a set of patterns that all align together */
export class AlignGroup
{
	// WARNING: this type is loaded from JSON which doesn't call the constructor.
	// That means functions and properties will be 'undefined'
	constructor(name: string = "", leading_space: number = 0, ...patterns:AlignPattern[])
	{
		this.name = name;
		this.leading_space = leading_space;
		this.patterns = patterns || [];
	}

	/** The name of the group */
	public readonly name :string;

	/** The number leading whitespaces the group should have */
	public readonly leading_space: number;

	/** Patterns belonging to the align group */
	public readonly patterns: AlignPattern[];
}
