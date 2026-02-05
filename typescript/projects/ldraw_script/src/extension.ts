/**
 * extension.ts
 * 
 * Entry point for the LDraw Script VS Code extension.
 * Registers the completion provider for .ldr files.
 */

import * as vscode from 'vscode';
import { LdrCompletionProvider } from './completionProvider';

let completionProvider: vscode.Disposable | undefined;

export function activate(context: vscode.ExtensionContext) {
    console.log('LDraw Script extension activated');

    // Register completion provider for .ldr files
    const provider = new LdrCompletionProvider(context.extensionPath);
    
    completionProvider = vscode.languages.registerCompletionItemProvider(
        { language: 'ldr', scheme: 'file' },
        provider,
        '*' // Trigger on * character
    );

    context.subscriptions.push(completionProvider);
}

export function deactivate() {
    if (completionProvider) {
        completionProvider.dispose();
    }
}
