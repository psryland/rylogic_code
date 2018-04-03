import {AlignPattern} from './align_pattern';

/**
 * Represents a set of patterns that all align together
 */
export class AlignGroup
{
	constructor(name: string = "", leading_space: number = 1, ...patterns:AlignPattern[])
	{
		this.name = name;
		this.leading_space = leading_space;
		this.patterns = patterns;
	}

	/** The name of the group */
	public name :string;

	/** The number leading whitespaces the group should have */
	public leading_space: number;

	/** Patterns belonging to the align group */
	public patterns: AlignPattern[];
}
