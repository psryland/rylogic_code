import * as vscode from 'vscode';
import { AlignGroup } from './align_group';

export class Aligner
{
	constructor()
	{
		// Read the alignment group patterns
		this.groups = vscode.workspace.getConfiguration('align').get('groups') || [];
		vscode.workspace.onDidChangeConfiguration(() =>
		{
			this.groups = vscode.workspace.getConfiguration('align').get('groups') || [];
		});
		
	}
	
	/** The alignment sets */
	public groups: AlignGroup[];

	/** Align text in the editor */
	DoAlign(editor: vscode.TextEditor) :void
	{
		var selection = editor.selection;
		var text = editor.document.getText(selection);
		vscode.window.showInformationMessage("Selection = "+text+" length = "+text.length);
	}
}