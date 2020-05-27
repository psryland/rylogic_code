import * as vscode from "vscode";
import { AlignGroup } from './align_group';
import { AlignPattern } from './align_pattern';
import { Range } from './range';

/** */
export class Token
{
	constructor(grp: AlignGroup, grp_index: number, patn_index: number, line: vscode.TextLine, line_number:number, span: Range, tab_size: number)
	{
		this.grp = grp;
		this.grp_index = grp_index;
		this.pattern = grp.patterns[patn_index];
		this.span = span;
		this.line = line;
		this.line_number = line_number;

		let line_text = line.text;
		this.MinCharIndex = line_text.substr(0, span.begi).replace(/\s*$/,'').length;
		this.MinColumnIndex = this.CharIndexToColumnIndex(line_text, this.MinCharIndex, tab_size);
		this.CurrentCharIndex = span.begi;
		this.CurrentColumnIndex = this.CharIndexToColumnIndex(line_text, span.begi, tab_size);
	}

	/** The align group corresponding to GrpIndex */
	public readonly grp: AlignGroup;

	/** The index position of the pattern in the priority list */
	public readonly grp_index: number;

	/** The pattern that this token matches */
	public readonly pattern: AlignPattern;

	/** The line that this token is on */
	public readonly line: vscode.TextLine;

	/** The line number that the token is on */
	public readonly line_number: number;

	/** The character range of the matched pattern on the line */
	public readonly span: Range;

	/** The minimum character index that this token can be left shifted to */
	public MinCharIndex: number;

	/** The minimum column index that this token can be left shifted to */
	public MinColumnIndex: number;

	/** The current char index of the token */
	public readonly CurrentCharIndex: number;

	/** The current column index of the token */
	public readonly CurrentColumnIndex: number;

	/** The minimum distance of this token from 'caret_pos' */
	public Distance(caret_pos: number): number
	{
		return this.span.contains(caret_pos) ? 0 
			: Math.min(Math.abs(this.span.begi - caret_pos), Math.abs(this.span.endi - caret_pos));
	}

	/** Set this edit so that it cannot be moved to the left */
	public SetNoLeftShift(): void
	{
		this.MinCharIndex = this.CurrentCharIndex;
		this.MinColumnIndex = this.CurrentColumnIndex;
	}

	/** Converts a char index into a column index for the given line */
	private CharIndexToColumnIndex(line: string, char_index: number, tab_size: number): number
	{
		// Careful, columns != char index because of tabs.
		// Also, Surrogate pairs are pairs of char's that only take up one column.
		let col = 0;
		for (let i = 0; i != char_index; ++i)
		{
			col += line[i] == '\t' ? tab_size - (col % tab_size) : 1;
			//if (i < line.length - 1 && char.IsSurrogatePair(line[i], line[i + 1]))
			//	++i;
		}
		return col;
	}

	/** ToString */
	public toString(): string
	{
		return `Line: ${this.line.lineNumber} Grp: ${this.grp} Patn: ${this.pattern} Span: ${this.span}`;
	}
}
