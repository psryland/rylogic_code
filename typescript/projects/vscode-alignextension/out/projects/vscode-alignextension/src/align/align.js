"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const vscode = require("vscode");
class Aligner {
    constructor() {
        // Read the alignment group patterns
        this.groups = vscode.workspace.getConfiguration('align').get('groups') || [];
        vscode.workspace.onDidChangeConfiguration(() => {
            this.groups = vscode.workspace.getConfiguration('align').get('groups') || [];
        });
    }
    /** Align text in the editor */
    DoAlign(editor) {
        var selection = editor.selection;
        var text = editor.document.getText(selection);
        vscode.window.showInformationMessage("Selection = " + text + " length = " + text.length);
    }
}
exports.Aligner = Aligner;
//# sourceMappingURL=align.js.map