import * as vscode from 'vscode';
import { AlignGroup } from './align_group';
import { AlignPattern } from './align_pattern';

export class Aligner
{
	constructor()
	{
		this.groups = vscode.workspace.getConfiguration('textaligner').get('groups') || [];
		vscode.workspace.onDidChangeConfiguration(() =>
		{
			this.groups = vscode.workspace.getConfiguration('textaligner').get('groups') || [];
		});
	}
	
	/** The alignment sets */
	public groups: AlignGroup[];

	/** Align text in the editor */
	DoAlign(editor: vscode.TextEditor) :void
	{
		let selection = editor.selection;

		// Test
		let text = editor.document.getText(selection);
		vscode.window.showInformationMessage("Selection = "+text+" length = "+text.length);

		// Get the pattern groups to align on
		let grps = this.ChoosePatterns(editor, selection);
		grps.forEach(_ =>
		{
		});
	}

	/** Select the pattern groups to align to */
	ChoosePatterns(editor: vscode.TextEditor, selection: vscode.Selection) :AlignGroup[]
	{
		// If there is a selection, see if we should use the selected text as the align pattern.
		// Only use single line, non-whole-line selections on the same line as the caret.
		if (this.IsPatternSelection(selection))
		{
			let expr = editor.document.getText(selection);
			let ofs = expr.search(/\S|$/); // Count leading whitespace
			expr = expr.substring(ofs);    // Strip leading whitespace
			expr = expr.replace(/[-\/\\^$*+?.()|[\]{}]/g, '\\$&'); // Escape the string 
			return [new AlignGroup("Selection", 0, new AlignPattern(expr, {offset: ofs}))];
		}

		// Make a copy of the alignment groups from settings
		let groups:AlignGroup[] = Object.assign([], this.groups);

		// If the cursor is next to an alignment pattern, move that pattern to the front of the priority list
		// Only do this when there isn't a multi line selection as the 'near-pattern' behaviour is confusing.
		if (selection.isEmpty)
		{
			//let line = selection.start.line;
			let line_text = editor.document.lineAt(selection.start.line).text;
			//let column = selection.active.character;
			//let column = selection.Pos.Begi - line.Start.Position;
			//	Debug.Assert(column >= 0 && column <= (line.End.Position - line.Start.Position));

			// Find matches that span, are immediately to the right, or immediately to the left (priority order)
			//let spanning:AlignGroup|null = null;
			//let rightof:AlignGroup|null = null;
			//let leftof:AlignGroup|null = null;
			for (let i = 0; i !== groups.length; ++i)
			{
				let grp = groups[i];
				grp.patterns.forEach(p =>
				{
					let patn = new RegExp(p.pattern);
					let matches = patn.exec(line_text);
					if (matches === null) return;
					matches.forEach(m =>
					{
						//// Spanning matches have the highest priority
						//if (m.Beg < column && m.End > column)
						//	spanning = grp;

						//// Matches to the right separated only by whitespace are the next highest priority
						//if (match.Beg >= column && string.IsNullOrWhiteSpace(line_text.Substring(column, match.Begi - column)))
						//	rightof = grp;

						//// Matches to the left separated only by whitespace are next
						//if (match.End <= column && string.IsNullOrWhiteSpace(line_text.Substring(match.Endi, column - match.Endi)))
						//	leftof = grp;
					});
				});

			}

			//// Move the 'near' patterns to the front of the priority list
			//if (leftof   !== null) { m_groups.Remove(leftof); m_groups.Insert(0, leftof); }
			//if (rightof  !== null) { m_groups.Remove(rightof); m_groups.Insert(0, rightof); }
			//if (spanning !== null) { m_groups.Remove(spanning); m_groups.Insert(0, spanning); }
		}

		return groups;
	}

	/** Return true if 'selection' represents a pattern to align to */
	IsPatternSelection(selection: vscode.Selection) :boolean
	{
		if (selection.isEmpty) return false;
		if (!selection.isSingleLine) return false;
		return true;
	}
}