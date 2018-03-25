'use strict';
Object.defineProperty(exports, "__esModule", { value: true });
// The module 'vscode' contains the VS Code extensibility API
// Import the module and reference it with the alias vscode in your code below
const vscode = require("vscode");
const align_1 = require("./align/align");
// this method is called when your extension is activated
// your extension is activated the very first time the command is executed
function activate(context) {
    // Use the console to output diagnostic information (console.log) and errors (console.error)
    // This line of code will only be executed once when your extension is activated
    console.log('vscode-alignextension is now active.');
    let aligner = new align_1.Aligner();
    // The command has been defined in the package.json file
    // Now provide the implementation of the command with 'registerCommand'
    // The commandId parameter must match the command field in package.json
    context.subscriptions.push(vscode.commands.registerCommand('extension.Align', () => {
        var editor = vscode.window.activeTextEditor;
        if (!editor)
            return;
        aligner.DoAlign(editor);
    }));
}
exports.activate = activate;
// this method is called when your extension is deactivated
function deactivate() {
    console.log('vscode-alignextension is now deactivated.');
}
exports.deactivate = deactivate;
//# sourceMappingURL=extension.js.map