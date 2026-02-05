/**
 * completionProvider.ts
 * 
 * Provides context-aware auto-completion for LDraw script.
 */

import * as vscode from 'vscode';
import * as path from 'path';
import * as fs from 'fs';

interface TemplateEntry {
    keyword: string;
    display: string;
    rootOnly: boolean;
    hidden: boolean;
    recursive: boolean;
    children: string[];
    references: string[];
    childReferences: string[];
}

interface TemplatesData {
    templates: Record<string, TemplateEntry>;
    rootKeywords: string[];
    objectTemplates: string[];
}

export class LdrCompletionProvider implements vscode.CompletionItemProvider {
    private templates: TemplatesData;

    constructor(extensionPath: string) {
        const templatesPath = path.join(extensionPath, 'src', 'templates.json');
        const content = fs.readFileSync(templatesPath, 'utf-8');
        this.templates = JSON.parse(content);
    }

    provideCompletionItems(
        document: vscode.TextDocument,
        position: vscode.Position,
        _token: vscode.CancellationToken,
        _context: vscode.CompletionContext
    ): vscode.CompletionItem[] {
        const context = this.getContext(document, position);
        const validKeywords = this.getValidKeywords(context);
        
        // Check if the character before cursor is '*'
        const charBefore = position.character > 0 
            ? document.getText(new vscode.Range(position.translate(0, -1), position))
            : '';
        const hasAsterisk = charBefore === '*';
        
        return validKeywords.map(keyword => this.createCompletionItem(keyword, hasAsterisk));
    }

    /**
     * Analyzes the document up to the cursor position to determine the current context.
     * Returns a stack of parent keywords (from outermost to innermost).
     */
    private getContext(document: vscode.TextDocument, position: vscode.Position): string[] {
        const text = document.getText(new vscode.Range(new vscode.Position(0, 0), position));
        
        const parentStack: string[] = [];
        let braceDepth = 0;
        let currentKeyword: string | null = null;
        
        // Simple state machine to track context
        // We need to handle: *Keyword, comments, strings, and braces
        let inLineComment = false;
        let inBlockComment = false;
        let inString = false;
        let i = 0;
        
        while (i < text.length) {
            const char = text[i];
            const nextChar = text[i + 1];
            
            // Handle newlines
            if (char === '\n') {
                inLineComment = false;
                i++;
                continue;
            }
            
            // Handle comments
            if (!inString && !inBlockComment && char === '/' && nextChar === '/') {
                inLineComment = true;
                i += 2;
                continue;
            }
            if (!inString && !inLineComment && char === '/' && nextChar === '*') {
                inBlockComment = true;
                i += 2;
                continue;
            }
            if (inBlockComment && char === '*' && nextChar === '/') {
                inBlockComment = false;
                i += 2;
                continue;
            }
            
            if (inLineComment || inBlockComment) {
                i++;
                continue;
            }
            
            // Handle strings
            if (char === '"' && (i === 0 || text[i - 1] !== '\\')) {
                inString = !inString;
                i++;
                continue;
            }
            
            if (inString) {
                i++;
                continue;
            }
            
            // Handle keywords: *Keyword or **Keyword or *!Keyword
            if (char === '*') {
                let keywordStart = i + 1;
                // Skip additional * or !
                while (keywordStart < text.length && (text[keywordStart] === '*' || text[keywordStart] === '!')) {
                    keywordStart++;
                }
                // Extract keyword
                let keywordEnd = keywordStart;
                while (keywordEnd < text.length && /\w/.test(text[keywordEnd])) {
                    keywordEnd++;
                }
                if (keywordEnd > keywordStart) {
                    currentKeyword = text.substring(keywordStart, keywordEnd);
                }
                i = keywordEnd;
                continue;
            }
            
            // Handle braces
            if (char === '{') {
                if (currentKeyword) {
                    parentStack.push(currentKeyword);
                    currentKeyword = null;
                }
                braceDepth++;
                i++;
                continue;
            }
            
            if (char === '}') {
                braceDepth--;
                if (braceDepth >= 0 && parentStack.length > 0) {
                    parentStack.pop();
                }
                i++;
                continue;
            }
            
            i++;
        }
        
        return parentStack;
    }

    /**
     * Returns keywords that are valid in the given context.
     */
    private getValidKeywords(context: string[]): string[] {
        if (context.length === 0) {
            // At root level: return all non-hidden templates
            return this.templates.rootKeywords.filter(k => {
                const template = this.templates.templates[k];
                return template && !template.hidden;
            });
        }
        
        // Inside a template: find valid children
        const parentKeyword = context[context.length - 1];
        const parent = this.templates.templates[parentKeyword];
        
        if (!parent) {
            // Unknown parent, return empty
            return [];
        }
        
        const validKeywords = new Set<string>();
        
        // Add direct children
        for (const child of parent.children) {
            validKeywords.add(child);
        }
        
        // Resolve @ references - @ref means template 'ref' is valid at this level
        for (const ref of parent.references) {
            validKeywords.add(ref);
        }
        
        // Resolve $ references (children of referenced template)
        for (const ref of parent.childReferences) {
            const refTemplate = this.templates.templates[ref];
            if (refTemplate) {
                for (const child of refTemplate.children) {
                    validKeywords.add(child);
                }
                // Also include @ references from the $ referenced template
                // @ref means the template 'ref' itself is valid at this level
                for (const subRef of refTemplate.references) {
                    validKeywords.add(subRef);
                }
            }
        }
        
        // If parent has recursive flag, add all object templates
        if (parent.recursive) {
            for (const objTemplate of this.templates.objectTemplates) {
                validKeywords.add(objTemplate);
            }
        }
        
        return Array.from(validKeywords);
    }

    /**
     * Creates a completion item for a keyword.
     * @param keyword The keyword to create a completion for
     * @param hasAsterisk Whether the user has already typed '*' before the cursor
     */
    private createCompletionItem(keyword: string, hasAsterisk: boolean): vscode.CompletionItem {
        const template = this.templates.templates[keyword];
        
        const item = new vscode.CompletionItem(`*${keyword}`, vscode.CompletionItemKind.Keyword);
        
        // If user already typed *, insert just the keyword; otherwise insert *keyword
        item.insertText = hasAsterisk ? keyword : `*${keyword}`;
        
        // Filter text without * - VS Code filters based on text after the trigger character
        item.filterText = keyword;
        
        // Show the full template in the detail/documentation
        if (template) {
            item.detail = template.display.split('\n')[0];
            if (template.display.includes('\n')) {
                item.documentation = new vscode.MarkdownString('```ldr\n' + template.display + '\n```');
            }
        }
        
        return item;
    }
}
