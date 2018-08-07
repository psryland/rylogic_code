import * as vscode from 'vscode';
import * as Math_ from "../../../../rylogic/src/maths/maths";
import * as Range_ from "../../../../rylogic/src/maths/range";
import { AlignGroup } from './align_group';
import { AlignPattern } from './align_pattern';
import { Token } from './token';
import { AlignPos } from './align_pos';
import Range = Math_.Range;

export class Aligner
{
	constructor()
	{
		this.groups = [];
		this.tab_size = 4;
		this.ValidateSettings();
		vscode.workspace.onDidChangeConfiguration(() => this.ValidateSettings());
	}
	
	/** The alignment sets */
	public groups: AlignGroup[];

	/** The tab size in spaces */
	public tab_size: number;

	/** Align text in the editor */
	DoAlign(editor: vscode.TextEditor) :void
	{
		let doc = editor.document;
		let selection = editor.selection;

		// Prioritise the pattern groups to align on
		let grps = this.PrioritisePatternGroups(doc, selection);

		// Find the edits to make
		let edits = this.FindAlignments(doc, selection, grps);

		// Make the alignment edits in an undo scope
		this.DoAligning(editor, edits);
	}

	/** Prioritise the alignment groups */
	PrioritisePatternGroups(doc: vscode.TextDocument, selection: vscode.Selection) :AlignGroup[]
	{
		// If there is a selection, see if we should use the selected text as the align pattern.
		// Only use single line, non-whole-line selections on the same line as the caret.
		if (this.IsPatternSelection(selection))
		{
			let expr = doc.getText(selection);
			let ofs = expr.search(/\S|$/); // Count leading whitespace
			expr = expr.substring(ofs);    // Strip leading whitespace
			expr = expr.replace(/[-\/\\^$*+?.()|[\]{}]/g, '\\$&')// Escape the string 
			return [new AlignGroup("Selection", 0, new AlignPattern(new RegExp(expr, 'g'), {offset: ofs}))];
		}

		// Make a copy of the alignment groups from settings
		let groups:AlignGroup[] = []
		for (let g of this.groups)
			groups.push(g)

		// If the cursor is next to an alignment pattern, move that pattern to the front of the priority list
		// Only do this when there isn't a multi line selection as the 'near-pattern' behaviour is confusing.
		if (selection.isEmpty)
		{
			let column = selection.active.character;
			let line = doc.lineAt(selection.start.line);
			let line_text = line.text;

			// Find matches that span, are immediately to the right, or immediately to the left (priority order)
			let spanning:AlignGroup|null = null;
			let rightof:AlignGroup|null = null;
			let leftof:AlignGroup|null = null;
			for (let g of groups)
			{
				for (let p of g.patterns)
				{
					for (let m; m = p.pattern.exec(line_text);)
					{
						let beg = m.index;
						let end = m.index + m[0].length;

						// Spanning matches have the highest priority
						if (beg < column && end > column)
							spanning = g;

						// Matches to the right separated only by whitespace are the next highest priority
						if (beg >= column && !line_text.substr(column, beg - column).trim())
							rightof = g;

						// Matches to the left separated only by whitespace are next
						if (end <= column && !line_text.substr(end, column - end).trim())
							leftof = g;
					}
				}
			}

			// Move the 'near' patterns to the front of the priority list
			if (leftof   !== null) { groups = groups.filter(x => x !== leftof); groups.unshift(leftof); }
			if (rightof  !== null) { groups = groups.filter(x => x !== rightof); groups.unshift(rightof); }
			if (spanning !== null) { groups = groups.filter(x => x !== spanning); groups.unshift(spanning); }
		}

		return groups;
	}

	/** Return a collection of the edits to make to do the aligning */
	FindAlignments(doc: vscode.TextDocument, selection: vscode.Selection, grps: AlignGroup[]) :Token[]
	{
		let edits:Token[] = [];
		let caret = selection.active.character;

		// If the selection spans multiple lines, limit the aligning to those lines.
		// If a whole single line is selected, treat that like multiple selected lines.
		// It means aligning won't do anything, but I think that's what a user would expect,
		// consistent with selecting more than 1 line.
		let line_range: Range = selection.isSingleLine //&& !selection.IsWholeLines
			? Range_.create(0, doc.lineCount - 1)
			: Range_.create(selection.start.line, selection.end.line);

		// Get the align boundaries on the current line
		let boundaries = this.FindAlignBoundariesOnLine(doc, selection.active.line, grps);

		// Sort the boundaries by pattern priority, then by distance from the caret
		boundaries.sort((l,r) =>
		{
			if (l.grp_index !== r.grp_index) return l.grp_index - r.grp_index;
			return l.Distance(caret) - r.Distance(caret);
		});

		// Find the first boundary that can be aligned
		for (let align of boundaries)
		{
			// Each time we come round, the previous 'align' should have resulted in nothing
			// to align so edits should always be empty here
			//Debug.Assert(edits.Count == 0);

			// Find the index of 'align' within 'boundaries' for 'align's pattern type
			let token_index :number = 0;
			for (let b of boundaries)
			{
				if (b === align) break;
				if (b.grp_index == align.grp_index) ++token_index;
			}

			// For each successive adjacent row, look for an alignment boundary at the same index
			for (let x of this.FindAlignmentEdits(doc, align, token_index, grps, -1, line_range)) edits.push(x);
			for (let x of this.FindAlignmentEdits(doc, align, token_index, grps, +1, line_range)) edits.push(x);

			// No edits to be made, means try the next alignment candidate
			if (edits.length == 0)
				continue;

			edits.unshift(align);

			// If there are edits but they are all already aligned at the
			// correct column, then move on to the next candidate.
			let pos = this.FindAlignColumn(edits);
			let col = pos.column - pos.span.begi;
			if (edits.every(x => x.CurrentColumnIndex - x.pattern.offset == col))
			{
				edits = [];
				continue;
			}

			break;
		}
		return edits;
	}

	/** Performs the aligning using the given edits */
	DoAligning(editor: vscode.TextEditor, edits: Token[]): void
	{
		// Nothing to do
		if (edits.length == 0)
			return;

		// The first edit is the line that the aligning is based on, if the
		// token we're aligning to is the first thing on the line don't align
		// to column zero, leave the leading whitespace as is
		let first_on_line :string|undefined = undefined
		if (edits[0].MinColumnIndex == 0)
			first_on_line = edits[0].line.text.substr(0, edits[0].line.firstNonWhitespaceCharacterIndex)

		// Sort in descending line order
		edits.sort((l,r) => r.line.lineNumber - l.line.lineNumber);

		// Find the column to align to
		let pos = this.FindAlignColumn(edits);
		let col = pos.column - pos.span.begi;

		// Create an undo scope
		editor.edit(ed =>
		{
			for (let edit of edits)
			{
				// Careful with order, we need to apply the edits assuming 'line' isn't changed with each one

				// Insert whitespace after the pattern if needed
				let ws_tail = Math.max(0, pos.span.endi - (edit.pattern.offset + edit.span.count));
				if (ws_tail > 0)
				{
					let p = edit.line.range.start.translate(0, edit.span.endi);
					ed.insert(p, ' '.repeat(ws_tail));
				}

				// Delete all preceding whitespace
				let ws_remove = new vscode.Range(
					edit.line.range.start.translate(0, edit.MinCharIndex),
					edit.line.range.start.translate(0, edit.span.begi));
				ed.delete(ws_remove);

				// Insert whitespace to align
				if (first_on_line)
				{
					let p = edit.line.range.start;
					ed.insert(p, first_on_line);
				}
				else
				{
					let ws_head = col - edit.MinColumnIndex + edit.pattern.offset;
					if (ws_head > 0)
					{
						let p = edit.line.range.start.translate(0, edit.MinCharIndex);
						ed.insert(p, ' '.repeat(ws_head));
					}
				}
			}
			
			return true;
		});
	}

	/** Searches above (dir == -1) or below (dir == +1) for alignment tokens that occur
	 * with the same token index as 'align'. Returns all found. */
	FindAlignmentEdits(doc: vscode.TextDocument, align: Token, token_index: number, grps: AlignGroup[], dir: number, line_range: Range): Token[]
	{
		let edits:Token[] = []
		for (let i = align.line.lineNumber + dir; Range_.ContainsInclusive(line_range, i); i += dir)
		{
			// Get the alignment boundaries on this line
			let boundaries = this.FindAlignBoundariesOnLine(doc, i, grps);

			// Look for a token that matches 'align' at 'token_index' position
			let idx: number = -1;
			let match: Token|null = null;
			for (let b of boundaries)
			{
				if (b.grp_index != align.grp_index) continue;
				if (++idx != token_index) continue;
				if ((b.MinCharIndex == 0) != (align.MinCharIndex == 0)) break; // Don't align things on their own line, with things that aren't on their own line
				match = b;
				break;
			}

			// No alignment boundary found, stop searching in this direction
			if (match == null)
				return edits;

			// Found an alignment boundary.
			edits.push(match);
		}
		return edits;
	}

	/** Scans the line as 'line_number' for alignment boundaries, returning a collection in the order they occur within the line */
	FindAlignBoundariesOnLine(doc: vscode.TextDocument, line_number: number, grps: AlignGroup[]): Token[]
	{
		let tokens: Token[] = [];
		if (line_number < 0 || line_number >= doc.lineCount)
			return tokens;

		// Read the line from the document
		let line = doc.lineAt(line_number);
		let line_text = line.text;

		// For each alignment group, test for alignment boundaries
		let grp_index:number = -1;
		for (let g of grps)
		{
			++grp_index;
			var patn_index:number = -1;
			for (let p of g.patterns)
			{
				++patn_index;
				for (let m; m = p.pattern.exec(line_text);)
				{
					let span = Range_.create(m.index, m.index + m[0].length);
					tokens.push(new Token(g, grp_index, patn_index, line, span, this.tab_size));
				}
			}
		}

		tokens.sort((l,r) => l.span.begi - r.span.begi);
		return tokens;
	}

	/** Returns the column index and range for aligning */
	FindAlignColumn(toks: Token[]): AlignPos
	{
		let min_column: number = 0;
		let leading_ws: number = 0;
		let span:Range = Range_.create(0,0); // include 0 in the range
		for (let tok of toks)
		{
			Range_.Encompass(span, tok.pattern.offset);
			Range_.Encompass(span, tok.pattern.offset + tok.pattern.minimum_width);
			min_column = Math.max(min_column, tok.MinColumnIndex);
			leading_ws = Math.max(leading_ws, tok.grp.leading_space);
		}
		if (min_column != 0) min_column += leading_ws; // Add leading whitespace, unless at column 0
		return new AlignPos(min_column, span);
	}

	/** Return true if 'selection' represents a pattern to align to */
	IsPatternSelection(selection: vscode.Selection) :boolean
	{
		if (selection.isEmpty) return false;
		if (!selection.isSingleLine) return false;
		return true;
	}

	/** Check that all patterns are valid RegExp's */
	ValidateSettings(): void
	{
		this.tab_size = vscode.workspace.getConfiguration('editor').get('tabSize') || 4;
		let groups:any[] = vscode.workspace.getConfiguration('textaligner').get('groups') || [];

		let err: string[] = [];
		for (let g of groups)
		{
			let grp = new AlignGroup(g.name, g.leading_space);
			for (let p of g.patterns)
			{
				try
				{
					let re = new RegExp(p.pattern, 'g');
					let patn = new AlignPattern(re, { offset: p.offset, min_width: p.minimum_width, comment: p.comment });
					grp.patterns.push(patn);
				}
				catch (error)
				{
					err.push(error);
				}
			}
			this.groups.push(grp);
		}
		if (err.length == 0)
			return;

		// Toast message
		vscode.window.showWarningMessage("Invalid regular expressions in 'textaligner.groups'", "" + err);
	}
}