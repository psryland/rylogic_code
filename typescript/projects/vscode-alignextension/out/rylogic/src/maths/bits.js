"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
/** Returns true if 'n' is a exact power of two */
function IsPowerOfTwo(n) {
    return Math.floor(n) == n && ((n - 1) & n) == 0;
}
exports.IsPowerOfTwo = IsPowerOfTwo;
//# sourceMappingURL=bits.js.map