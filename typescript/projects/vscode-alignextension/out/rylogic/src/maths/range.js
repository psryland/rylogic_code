"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
class RangeImpl {
    constructor() {
        this.beg = Number.POSITIVE_INFINITY;
        this.end = Number.NEGATIVE_INFINITY;
    }
    /** Return the centre of the range */
    get centre() {
        return (this.beg + this.end) * 0.5;
    }
    /** Return the size of range, i.e. end - beg */
    get size() {
        return this.end - this.beg;
    }
    /** Return the beginning of the range, rounded to an integer */
    get begi() {
        return Math.floor(this.beg);
    }
    /** Return the end of the range, rounded to an integer */
    get endi() {
        return Math.floor(this.end);
    }
    /** Return the span of this range as an integer */
    get count() {
        return this.endi - this.begi;
    }
}
/**
 * Create an new, invalid, range: [+MAX_VALUE,-MAX_VALUE)
 */
function create(beg, end) {
    let out = new RangeImpl();
    if (beg === undefined)
        beg = Number.POSITIVE_INFINITY;
    if (end === undefined)
        end = Number.NEGATIVE_INFINITY;
    return set(out, beg, end);
}
exports.create = create;
/**
 * Set the components of a range
 * @param range
 * @param beg
 * @param end
 */
function set(range, beg, end) {
    range.beg = beg;
    range.end = end;
    return range;
}
exports.set = set;
/**
 * Create a new copy of 'range'
 * @param range The source range to copy
 * @param out Where to write the result
 * @returns The clone of 'range'
 */
function clone(range, out) {
    out = out || create();
    if (range !== out) {
        out.beg = range.beg;
        out.end = range.end;
    }
    return out;
}
exports.clone = clone;
/**
 * Returns true if 'value' is within the range [beg,end) (i.e. end exclusive)
 * @param range The range to test for containing 'value'
 * @param value The number to test for being within 'range'
 * @returns True if 'value' is within 'range'
 */
function Contains(range, value) {
    return range.beg <= value && value < range.end;
}
exports.Contains = Contains;
/**
 * Returns true if 'value' is within the range [Begin,End] (i.e. end inclusive)
 * @param range The range to test for containing 'value'
 * @param value The number to test for being within 'range'
 * @returns True if 'value' is within 'range'
 */
function ContainsInclusive(range, value) {
    return range.beg <= value && value <= range.end;
}
exports.ContainsInclusive = ContainsInclusive;
/**
 * Compare two ranges</summary>
 * -1 if 'range' is less than 'value'
 * +1 if 'range' is greater than 'value'
 * Otherwise 0.
 * @param range
 * @param value
 * @returns -1 if 'range' < 'value', +1 if 'range' > 'value, 0 otherwise
 */
function Compare(range, value) {
    return range.end <= value ? -1 : range.beg > value ? +1 : 0;
}
exports.Compare = Compare;
/**
 * Grow the bounds of 'range' to include 'value'
 * @param range The range that is modified to include 'value'
 * @param value The number of include in the range
 */
function Encompass(range, value) {
    range.beg = Math.min(range.beg, value);
    range.end = Math.max(range.end, value);
}
exports.Encompass = Encompass;
/**
 * Grow the bounds of 'range0' to include 'range1'
 * @param {Range} range0
 * @param {Range} range1
 */
function EncompassRange(range0, range1) {
    range0.beg = Math.min(range0.beg, range1.beg);
    range0.end = Math.max(range0.end, range1.end);
}
exports.EncompassRange = EncompassRange;
/**
 * Returns a range that is the union of 'lhs' with 'rhs'
 * (basically the same as 'Encompass' except 'lhs' isn't modified.
 * @param lhs
 * @param rhs
 */
function Union(lhs, rhs) {
    if (lhs.size < 0)
        throw new Error("Left range is inside out");
    if (rhs.size < 0)
        throw new Error("Right range is inside out");
    return create(Math.min(lhs.beg, rhs.beg), Math.max(lhs.end, rhs.end));
}
exports.Union = Union;
/**
 * Returns the intersection of 'lhs' with 'rhs'.
 * If there is no intersection, returns [lhs.beg, lhs.beg) or [lhs.end,lhs.end).
 * Note: this means A.Intersect(B) != B.Intersect(A)
 * @param lhs
 * @param rhs
 */
function Intersect(lhs, rhs) {
    if (lhs.size < 0)
        throw new Error("Left range is inside out");
    if (rhs.size < 0)
        throw new Error("Right range is inside out");
    if (rhs.end <= lhs.beg)
        return create(lhs.beg, lhs.beg);
    if (rhs.beg >= lhs.end)
        return create(lhs.end, lhs.end);
    return create(Math.max(lhs.beg, rhs.beg), Math.min(lhs.end, rhs.end));
}
exports.Intersect = Intersect;
//# sourceMappingURL=range.js.map