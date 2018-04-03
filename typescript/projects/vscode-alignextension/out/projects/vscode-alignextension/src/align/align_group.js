"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
/**
 * Represents a set of patterns that all align together
 */
class AlignGroup {
    constructor(name = "", leading_space = 1, ...patterns) {
        this.name = name;
        this.leading_space = leading_space;
        this.patterns = patterns;
    }
}
exports.AlignGroup = AlignGroup;
//# sourceMappingURL=align_group.js.map