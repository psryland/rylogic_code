import * as vscode from 'vscode';
import { Range } from './range';

export class Selection
{
	// Notes:
	//  - vscode.Position.character is the line character index, not the character index in the file
	//  - vscode.Position.line is the line index

	constructor(selection: vscode.Selection, doc: vscode.TextDocument)
	{
		this.selection = selection;
		this.doc = doc;
	}

	/** The selected text */
	private readonly selection :vscode.Selection;

	/** The document that the selection is within */
	private readonly doc :vscode.TextDocument;

	/** The line character index of the start of the selection */
	get beg(): vscode.Position
	{
		return this.selection.start;
	}

	/** The line character index of the end of the selection */
	get end(): vscode.Position
	{
		return this.selection.end;
	}

	/** Get the current caret position */
	get caret() :vscode.Position
	{
		return this.selection.active;
	}

	/** The character index of the caret on the line it's on */
	get line_char_index() :number
	{
		return this.caret.character;
	}

	/** Get the first line of the selection */
	get sline(): vscode.TextLine
	{
		return this.doc.lineAt(this.selection.start.line);
	}

	/** Get the last line of the selection */
	get eline(): vscode.TextLine
	{
		return this.doc.lineAt(this.selection.end.line);
	}

	/** The lines contained in the selection [First,Last] */
	get lines(): Range
	{
		return new Range(this.selection.start.line, this.selection.end.line);
	}

	/** True if there is no selected text, just a single caret position */
	get is_empty() :boolean
	{
		return this.selection.isEmpty;
	}

	/** True if the selection is entirely on a single line */
	get is_single_line() :boolean
	{
		return this.selection.isSingleLine;
	}

	/// <summary>True if the selection spans whole lines</summary>
	get is_whole_lines() :boolean
	{
		let is_whole =
			this.selection.start.character == 0 &&
			this.selection.end.character >= this.eline.range.end.character; // >= because ELine.End.Position doesn't include the newline

		return is_whole || false;
	}
}