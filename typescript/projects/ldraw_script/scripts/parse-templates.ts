/**
 * parse-templates.ts
 * 
 * Build-time script that parses ldraw_templates.cpp and generates
 * a JSON file containing the template hierarchy for auto-completion.
 */

import * as fs from 'fs';
import * as path from 'path';

// Relative path from this script to ldraw_templates.cpp
const TEMPLATES_CPP_PATH = path.resolve(__dirname, '../../../../projects/rylogic/view3d-12/src/ldraw/ldraw_templates.cpp');
const OUTPUT_PATH = path.resolve(__dirname, '../src/templates.json');

interface TemplateEntry {
    /** The keyword (e.g., "*Box", "**o2w") */
    keyword: string;
    /** Display string showing full syntax */
    display: string;
    /** Whether this is a root-only keyword (*!) */
    rootOnly: boolean;
    /** Whether this is a hidden template (**) */
    hidden: boolean;
    /** Whether this template allows recursive nesting of object templates (&Recursive) */
    recursive: boolean;
    /** Child keywords that can appear inside this template's braces */
    children: string[];
    /** References to other templates via @ */
    references: string[];
    /** References to children of other templates via $ */
    childReferences: string[];
}

interface TemplatesData {
    /** All templates indexed by keyword (without prefix) */
    templates: Record<string, TemplateEntry>;
    /** Keywords valid at root level */
    rootKeywords: string[];
    /** Object templates (non-hidden, non-rootOnly) that can be nested recursively */
    objectTemplates: string[];
}

function parseTemplatesCpp(content: string): TemplatesData {
    const templates: Record<string, TemplateEntry> = {};
    const rootKeywords: string[] = [];

    // Extract the string content from str.append("...") calls
    // Match content between quotes, handling escaped characters
    const stringLiteralRegex = /"((?:[^"\\]|\\.)*)"/g;
    
    // Concatenate all string literals
    let fullContent = '';
    let match;
    while ((match = stringLiteralRegex.exec(content)) !== null) {
        // Unescape the string
        let str = match[1]
            .replace(/\\n/g, '\n')
            .replace(/\\t/g, '\t')
            .replace(/\\"/g, '"')
            .replace(/\\\\/g, '\\');
        fullContent += str;
    }

    // Parse the concatenated template content
    const lines = fullContent.split('\n');
    let currentTemplate: TemplateEntry | null = null;
    let braceDepth = 0;
    let templateStack: TemplateEntry[] = [];

    for (const line of lines) {
        const trimmed = line.trim();
        if (!trimmed) continue;

        // Check for template definition: *Keyword, **Keyword, or *!Keyword
        const templateMatch = trimmed.match(/^(\*\*|\*!|\*)(\w+)(.*)$/);
        if (templateMatch && braceDepth === 0) {
            const prefix = templateMatch[1];
            const keyword = templateMatch[2];
            const rest = templateMatch[3].trim();
            
            const isHidden = prefix === '**';
            const isRootOnly = prefix === '*!';
            
            // Build display string (keyword + rest of first line)
            let display = `*${keyword}${rest}`;
            
            currentTemplate = {
                keyword: keyword,
                display: display,
                rootOnly: isRootOnly,
                hidden: isHidden,
                recursive: false,
                children: [],
                references: [],
                childReferences: []
            };
            
            templates[keyword] = currentTemplate;
            
            if (!isHidden) {
                rootKeywords.push(keyword);
            }
        }

        // Track brace depth and collect children
        if (currentTemplate) {
            const openBraces = (trimmed.match(/\{/g) || []).length;
            const closeBraces = (trimmed.match(/\}/g) || []).length;
            
            if (braceDepth > 0 || openBraces > 0) {
                // Look for child keywords inside braces: [*Keyword or (*Keyword or just *Keyword
                const childMatches = trimmed.matchAll(/[\[\(]?\*(\w+)/g);
                for (const childMatch of childMatches) {
                    const childKeyword = childMatch[1];
                    if (childKeyword !== currentTemplate.keyword && 
                        !currentTemplate.children.includes(childKeyword)) {
                        currentTemplate.children.push(childKeyword);
                    }
                }

                // Look for @ references
                const refMatches = trimmed.matchAll(/@(\w+)/g);
                for (const refMatch of refMatches) {
                    const refName = refMatch[1];
                    if (!currentTemplate.references.includes(refName)) {
                        currentTemplate.references.push(refName);
                    }
                }

                // Look for $ references
                const childRefMatches = trimmed.matchAll(/\$(\w+)/g);
                for (const refMatch of childRefMatches) {
                    const refName = refMatch[1];
                    if (!currentTemplate.childReferences.includes(refName)) {
                        currentTemplate.childReferences.push(refName);
                    }
                }

                // Look for &Recursive marker
                if (trimmed.includes('&Recursive')) {
                    currentTemplate.recursive = true;
                }

                // Append to display for multi-line templates (first few lines)
                if (braceDepth === 0 && openBraces > 0) {
                    // Starting brace line
                    currentTemplate.display += '\n' + trimmed;
                } else if (braceDepth > 0 && braceDepth <= 2) {
                    // Content lines (limit depth to keep display manageable)
                    currentTemplate.display += '\n' + line;
                }
            }

            braceDepth += openBraces - closeBraces;
            
            if (braceDepth === 0 && closeBraces > 0) {
                // Template complete
                currentTemplate = null;
            }
        }
    }

    // Build list of object templates (non-hidden, non-rootOnly single * templates)
    const objectTemplates = rootKeywords.filter(k => {
        const template = templates[k];
        return template && !template.hidden && !template.rootOnly;
    });

    return { templates, rootKeywords, objectTemplates };
}

function resolveChildReferences(data: TemplatesData): void {
    // Resolve $references to include children from referenced templates
    for (const template of Object.values(data.templates)) {
        for (const ref of template.childReferences) {
            const refTemplate = data.templates[ref];
            if (refTemplate) {
                // Add all children from the referenced template
                for (const child of refTemplate.children) {
                    if (!template.children.includes(child)) {
                        template.children.push(child);
                    }
                }
                // Also add @ references from the referenced template
                for (const childRef of refTemplate.references) {
                    if (!template.references.includes(childRef)) {
                        template.references.push(childRef);
                    }
                }
            }
        }
    }
}

function main(): void {
    console.log(`Reading templates from: ${TEMPLATES_CPP_PATH}`);
    
    if (!fs.existsSync(TEMPLATES_CPP_PATH)) {
        console.error(`Error: Templates file not found at ${TEMPLATES_CPP_PATH}`);
        process.exit(1);
    }

    const content = fs.readFileSync(TEMPLATES_CPP_PATH, 'utf-8');
    const data = parseTemplatesCpp(content);
    
    // Resolve $ references
    resolveChildReferences(data);

    // Ensure output directory exists
    const outDir = path.dirname(OUTPUT_PATH);
    if (!fs.existsSync(outDir)) {
        fs.mkdirSync(outDir, { recursive: true });
    }

    fs.writeFileSync(OUTPUT_PATH, JSON.stringify(data, null, 2));
    
    console.log(`Generated ${OUTPUT_PATH}`);
    console.log(`  - ${Object.keys(data.templates).length} templates`);
    console.log(`  - ${data.rootKeywords.length} root keywords`);
}

main();
