'use strict';

// The module 'vscode' contains the VS Code extensibility API
// Import the module and reference it with the alias vscode in your code below
import * as vscode from 'vscode';
import {Aligner} from './align/align';

// this method is called when your extension is activated
// your extension is activated the very first time the command is executed
export function activate(context: vscode.ExtensionContext)
{
	// Use the console to output diagnostic information (console.log) and errors (console.error)
	// This line of code will only be executed once when your extension is activated
	console.log('vscode-alignextension is now active.');

	let aligner = new Aligner();

	// The command has been defined in the package.json file
	// Now provide the implementation of the command with 'registerCommand'
	// The commandId parameter must match the command field in package.json
	context.subscriptions.push(vscode.commands.registerCommand('extension.Align', () =>
	{
		var editor = vscode.window.activeTextEditor;
		if (!editor)
			return;

		aligner.DoAlign(editor);
	}));
}

// this method is called when your extension is deactivated
export function deactivate()
{
	console.log('vscode-alignextension is now deactivated.');
}