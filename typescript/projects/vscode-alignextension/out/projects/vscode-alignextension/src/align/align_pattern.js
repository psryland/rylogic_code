"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const Range_ = require("../../../../rylogic/src/maths/range");
/**
 * A pattern representing an alignment candidate
 */
class AlignPattern {
    constructor(obj = {}) {
        let { patn = /./, offset = 0, min_width = 0, comment = "", } = obj;
        this.patn = patn;
        this.offset = offset;
        this.min_width = min_width;
        this.comment = comment;
    }
    /** Gets the range of characters this pattern should occupy, relative to the aligning column */
    get position() {
        return Range_.create(this.offset, this.offset + this.min_width);
    }
}
exports.AlignPattern = AlignPattern;
//# sourceMappingURL=align_pattern.js.map