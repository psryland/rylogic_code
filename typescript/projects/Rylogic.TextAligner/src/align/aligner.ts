import * as vscode from 'vscode';
import { AlignGroup } from './align_group';
import { AlignPattern } from './align_pattern';
import { Selection } from './selection';
import { Token } from './token';
import { AlignPos } from './align_pos';
import { EAction } from './eaction';
import { EAlignCharacters } from './ealign_characters';
import { Range } from './range';

export class Aligner
{
	constructor()
	{
		// Set defaults
		this.groups = [];
		this.tab_size = 4;
		this.style = EAlignCharacters.Spaces;

		// Populate the groups list
		this.ValidateSettings();

		// Sign up for notification when the settings change
		vscode.workspace.onDidChangeConfiguration(() => this.ValidateSettings());
	}
	
	/** The alignment sets */
	public groups: AlignGroup[];

	/** The tab size in spaces */
	public tab_size: number;

	/** The whitespace characters to use for alignment */
	public style: EAlignCharacters;

	/** Align text in the editor */
	DoAlign(editor: vscode.TextEditor, action: EAction) :void
	{
		let doc = editor.document;
		let sel = new Selection(editor.selection, doc);

		// Prioritise the pattern groups to align on
		let grps = this.PrioritisePatternGroups(sel);

		// Find the edits to make
		let [edits, fallback_line_span] = this.FindAlignments(doc, sel, grps, action);

		// Make the alignment edits
		if (edits.length != 0)
		{
			switch (action)
			{
			case EAction.Align:
				this.DoAligning(editor, edits);
				break;
			case EAction.Unalign:
				this.DoUnaligning(editor, edits);
				break;
			}
		}
		else if (action == EAction.Unalign)
		{
			// If there is no selection then use the 'fallback_line_span' if available. This provides behaviour that the user
			// would otherwise interpret as a bug. Unalignment, when an alignment pattern is used, removes trailing whitespace
			// from all rows affected by the aligning. When there is nothing left to unalign, the user would still expected
			// trailing whitespace to be removed from a block that would otherwise be (un)aligned.
			let line_range = (sel.is_empty && fallback_line_span != null)
				? fallback_line_span : sel.lines;

			this.UnalignSelection(editor, doc, line_range);
		}
	}

	/** Prioritise the alignment groups */
	PrioritisePatternGroups(sel: Selection) :AlignGroup[]
	{
		// If there is a selection, see if we should use the selected text at the align pattern
		// Only use single line, non-whole-line selections on the same line as the caret
		let is_pattern_selection = !sel.is_empty && sel.is_single_line && !sel.is_whole_lines;
		if (is_pattern_selection)
		{
			// Create an alignment group based on the selection
			let text = sel.sline.text;
			let expr0 = text.substring(sel.beg.character, Math.min(sel.end.character, text.length));
			let expr1 = expr0.trimLeft();                               // Strip leading whitespace
			let ofs = expr0.length - expr1.length;                      // Count leading whitespace
			let expr = expr1.replace(/[-\/\\^$*+?.()|[\]{}]/g, '\\$&'); // Escape the string

			// Return the temporary pattern
			let patn = new RegExp(expr, 'g');
			let ap = new AlignPattern(patn, {offset: ofs})
			let ag = new AlignGroup("Selection", 0, ap);
			return [ag];
		}

		// Make a (shallow) copy of the alignment groups from settings
		let groups:AlignGroup[] = [...this.groups]

		// If the cursor is next to an alignment pattern, move that pattern to the front of the priority list.
		// Only do this when there isn't a multi line selection as the 'near-pattern' behaviour is confusing.
		if (sel.is_empty)
		{
			let line = sel.sline;
			let line_text = line.text;
			let line_char = sel.line_char_index;
			//assert(column >= 0 && line.range.contains(column))

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
						if (beg < line_char && end > line_char)
							spanning = g;

						// Matches to the right separated only by whitespace are the next highest priority
						if (beg >= line_char && !line_text.substr(line_char, beg - line_char).trim())
							rightof = g;

						// Matches to the left separated only by whitespace are next
						if (end <= line_char && !line_text.substr(end, line_char - end).trim())
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
	FindAlignments(doc: vscode.TextDocument, sel: Selection, grps: AlignGroup[], action: EAction) :[Token[], Range|null]
	{
		// If the selection spans multiple lines, limit the aligning to those lines.
		// If a whole single line is selected, treat that like multiple selected lines.
		// It means aligning won't do anything, but I think that's what a user would expect,
		// consistent with selecting more than 1 line.
		let line_range: Range = sel.is_single_line && !sel.is_whole_lines
			? new Range(0, doc.lineCount - 1)
			: sel.lines;

		// Get the align boundaries on the current line
		let boundaries = this.FindAlignBoundariesOnLine(doc, sel.caret.line, grps);

		// Sort the boundaries by pattern priority, then by distance from column 0
		let ordered = [...boundaries]
		ordered =
			action == EAction.Align   ? ordered.sort((l,r) => l.grp_index !== r.grp_index ? l.grp_index - r.grp_index : l.CurrentCharIndex - r.CurrentCharIndex) :
			action == EAction.Unalign ? ordered.sort((l,r) => l.grp_index !== r.grp_index ? r.grp_index - l.grp_index : l.CurrentCharIndex - r.CurrentCharIndex) :
			[];

		// Find the first boundary that can be aligned
		let edits:Token[] = [];
		let fallback_line_span:Range|null = null;
		for (let align of ordered)
		{
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

			let pos = this.FindAlignColumn(edits, 0);
			let all_first_on_line = edits.every(x => x.MinColumnIndex == 0);
			let col = all_first_on_line ? align.CurrentColumnIndex - align.pattern.offset : pos.column - pos.span.begi;

			switch (action)
			{
			// For aligning we want the highest priority group that is not already aligned.
			case EAction.Align:
				{
					// If there are edits but they are all already aligned at the
					// correct column, then move on to the next candidate.
					let already_aligned = edits.every(x => x.CurrentColumnIndex - x.pattern.offset == col);
					if (already_aligned)
					{
						edits = [];
						continue;
					}
					break;
				}

			// For unaligning we want the lowest priority group that is not currently 'unaligned'.
			case EAction.Unalign:
				{
					// If there are edits but they are all already the leading space distance
					// from their minimum column, then they are all 'unaligned', move on to the next candidate.
					let already_unaligned = edits.every(x => x.CurrentColumnIndex - x.MinColumnIndex == x.grp.leading_space);
					if (already_unaligned)
					{
						// Determine the range of lines spanned by the highest priority group
						fallback_line_span = new Range();
						for (let e of edits) fallback_line_span.encompass(e.line_number);
						edits = [];
						continue;
					}
					break;
				}
			}

			// Found the best match
			break;
		}
		return [edits, fallback_line_span];
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
		let grp_index: number = -1;
		for (let g of grps)
		{
			++grp_index;
			let patn_index: number = -1;
			for (let p of g.patterns)
			{
				++patn_index;
				for (let m; m = p.pattern.exec(line_text);) // check this....
				{
					let span = new Range(m.index, m.index + m[0].length);
					tokens.push(new Token(g, grp_index, patn_index, line, line_number, span, this.tab_size));
				}
			}
		}

		tokens.sort((l,r) => l.span.begi - r.span.begi);
		return tokens;
	}

	/** Searches above (dir == -1) or below (dir == +1) for alignment tokens that occur
	 * with the same token index as 'align'. Returns all found. */
	FindAlignmentEdits(doc: vscode.TextDocument, align: Token, token_index: number, grps: AlignGroup[], dir: number, line_range: Range): Token[]
	{
		let edits: Token[] = []
		for (let i = align.line.lineNumber + dir; line_range.contains_inclusive(i); i += dir)
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

	/** Returns the column index and range for aligning */
	FindAlignColumn(toks: Token[], min_column: number): AlignPos
	{
		let leading_ws: number = 0;
		let span:Range = new Range(0,0); // include 0 in the range
		let all_line_starts = true;
		for (let tok of toks)
		{
			span.encompass_range(tok.pattern.position);
			all_line_starts = all_line_starts && (tok.MinColumnIndex == 0);
			min_column = Math.max(min_column, tok.MinColumnIndex);
			leading_ws = Math.max(leading_ws, tok.grp.leading_space);
		}

		// Add the leading whitespace to the minimum column, unless the
		// alignment is the first non-whitespace character on the line.
		if (!all_line_starts)
		{
			min_column += leading_ws;

			// Round up to the next tab boundary
			if (this.style  == EAlignCharacters.Tabs && (min_column % this.tab_size) != 0)
				min_column += this.tab_size - (min_column % this.tab_size);
		}
		return new AlignPos(min_column, span);
	}

	/** Performs the aligning using the given edits */
	DoAligning(editor: vscode.TextEditor, edits: Token[]): void
	{
		// 'edits[0]' is the line that the aligning is based on, if the token we're aligning to is the
		// first thing on the line don't align to column zero, leave the leading whitespace as is.
		let min_column = edits[0].MinColumnIndex != 0 ? 0 : edits[0].CurrentColumnIndex;
		let leading_ws = edits[0].line.text.substr(0, edits[0].CurrentCharIndex);

		// Sort in descending line order so that the edits are applied from end to start,
		// meaning character index positions aren't invalidated as each line is edited.
		edits.sort((l,r) => r.line.lineNumber - l.line.lineNumber);

		// Find the column to align to
		let pos = this.FindAlignColumn(edits, min_column);
		let col = pos.column - pos.span.begi;

		// Create an undo scope
		editor.edit(ed =>
		{
			// Align each line to 'pos'
			for (let edit of edits)
			{
				// Careful with order, this order is choosen so that edits are
				// applied from right to left, preventing indices being invalidated.

				// Insert whitespace after the pattern if needed
				let ws_tail = Math.max(0, pos.span.endi - (edit.pattern.offset + edit.span.count));
				if (ws_tail > 0)
				{
					// Always use space characters for trailing padding
					let ins = edit.line.range.start.translate(0, edit.span.endi);
					ed.insert(ins, ' '.repeat(ws_tail));
				}

				// Delete all preceding whitespace
				ed.delete(new vscode.Range(
					edit.line.range.start.translate(0, edit.MinCharIndex),
					edit.line.range.start.translate(0, edit.span.begi)));
				

				// Create the aligning whitespace based on the alignment style
				let ws: string = "";
				if (min_column != 0)
				{
					// Copy the whitespace from the aligning line
					ws = leading_ws;
				}
				else
				{
					switch (this.style)
					{
					case EAlignCharacters.Spaces:
						{
							// In 'spaces' mode, simply pad from MinColumnIndex to 'col', adjusting for offset
							let count = col - edit.MinColumnIndex + edit.pattern.offset;
							ws = ' '.repeat(count);
							break;
						}
					case EAlignCharacters.Tabs:
						{
							// In 'tabs' mode, the alignment column 'col' will be a multiple of the tab size.
							// Add tabs between 'MinColumnIndex' and 'col', then adjust by 'Offset' using spaces if needed.
							// Note: Don't simplify, 'tab_count' relies on integer truncation here.
							let tab_count = ((col - edit.MinColumnIndex + this.tab_size - 1) / this.tab_size) + (edit.pattern.offset / this.tab_size);
							let spc_count = (this.tab_size + edit.pattern.offset % this.tab_size) % this.tab_size;
							ws = '\t'.repeat(tab_count) + ' '.repeat(spc_count);
							break;
						}
					case EAlignCharacters.Mixed:
						{
							// In 'mixed' mode. the alignment column 'col' will be the same as for 'spaces' mode.
							// Insert tabs up to the nearest tab boundary, then spaces after that.
							// Note: Don't simplify, 'tab_count' relies on integer truncation here.
							let tab_count = (col / this.tab_size) - (edit.MinColumnIndex / this.tab_size) + (edit.pattern.offset / this.tab_size);
							let spc_count = (col + edit.pattern.offset) % this.tab_size;
							if (tab_count == 0) spc_count -= (edit.MinColumnIndex % this.tab_size);
							ws = '\t'.repeat(tab_count) + ' '.repeat(spc_count);
							break;
						}
					default:
						{
							throw new Error("Unsupported whitespace style");
						}
					}
				}

				// Insert whitespace to align
				if (ws.length != 0)
				{
					let ins = edit.line.range.start.translate(0, edit.MinCharIndex);
					ed.insert(ins, ws);
				}
			}

			// Commit edit
			return true;
		});
	}

	/** Performs the unaligning using the given edits */
	DoUnaligning(editor: vscode.TextEditor, edits: Token[]): void
	{
		// Sort in descending line order so that the edits are applied from end to start,
		// meaning character index positions aren't invalidated as each line is edited.
		edits.sort((l, r) => r.line_number - l.line_number);

		// Create an undo scope
		editor.edit(ed =>
		{
			// 'Unalign' each line prior to the alignment group
			for (let edit of edits)
			{
				// Trim trailing whitespace
				let str = edit.line.text;
				let i = str.length;
				for (; i-- != 0 && this.IsWhiteSpace(str[i]);) { }
				if (++i != str.length)
				{
					ed.delete(new vscode.Range(
						edit.line.range.start.translate(0, i),
						edit.line.range.start.translate(0, str.length)));
				}

				// Delete the white space before the alignment pattern.
				ed.delete(new vscode.Range(
					edit.line.range.start.translate(0, edit.MinCharIndex),
					edit.line.range.start.translate(0, edit.CurrentCharIndex)));
				
				// Insert a single character if the pattern doesn't have leading white space, and isn't the start of a line
				if (edit.grp.leading_space != 0 && edit.MinCharIndex != 0)
				{
					let ins = edit.line.range.start.translate(0, edit.MinCharIndex)
					ed.insert(ins, " ");
				}
			}

			// Commit edit
			return true;
		});
	}

	/** Unalign selected text */
	UnalignSelection(editor: vscode.TextEditor, doc: vscode.TextDocument, line_range: Range): void
	{
		// Create an undo scope
		editor.edit(ed =>
		{
			// The character used to identify literal strings.
			// Null when not within a literal string.
			let quote: string|null = null;

			// Process lines in reverse order
			for (let j = line_range.endi; j >= line_range.begi; --j)
			{
				let line = doc.lineAt(j);
				let str = line.text;

				// Process text within each line in reverse order
				let s = str.length, e = s;
				for (let i = s; i-- != 0;)
				{
					if (quote != null)
					{
						// If currently within a literal string...
						// Search for the starting quote (ignoring escaped quotes)
						if (str[i] == quote && (i == 0 || str[i - 1] != '\\'))
							quote = null; // not within a string any more

						s = e = i;
					}
					else if (!this.IsWhiteSpace(str[i]))
					{
						// Replace consecutive white-space with a single whitespace character
						if (s != e)
						{
							// Remove all white space if at the end of a line,
							// otherwise replace with a single whitespace character
							ed.delete(new vscode.Range(
								line.range.start.translate(0, s),
								line.range.start.translate(0, e)));
							if (e != str.length)
								ed.insert(line.range.start.translate(0, s), " ");
						}

						// Found the end of a literal string
						if (quote == null && (str[i] == '"' || str[i] == '\''))
							quote = str[i];

						s = e = i;
					}
					else
					{
						// Consecutive whitespace
						s = i;
					}
				}

				// If the whole line is white space (and not part of a literal string), delete all of it
				if (e == str.length && quote == null)
				{
					ed.delete(new vscode.Range(
						line.range.start.translate(0, 0),
						line.range.start.translate(0, e)));
				}
			}

			// Commit edit
			return true;
		});
	}

	/** Check that all patterns are valid RegExp's */
	ValidateSettings(): void
	{
		// Read the alignment characters
		const style: string = vscode.workspace.getConfiguration('editor').get('align_characters') || "Spaces";
		this.style = EAlignCharacters[style as keyof typeof EAlignCharacters];

		// Read the tab size from settings
		this.tab_size = vscode.workspace.getConfiguration('editor').get('tabSize') || 4;

		// Collect all errors into a list
		let err: string[] = [];

		// Create a temporary array of alignment groups while we validate them
		let groups:any[] = vscode.workspace.getConfiguration('textaligner').get('groups') || [];
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

			// Add each valid group
			this.groups.push(grp);
		}
		if (err.length == 0)
			return;

		// Toast message
		vscode.window.showWarningMessage("Invalid regular expressions in 'textaligner.groups'", "" + err);
	}

	/** IsWhitespace predicate */
	private IsWhiteSpace(c:string): boolean
	{
		return c == ' ' || c == '\t' || c == '\n' || c == '\r' || c == '\v';
	}
}