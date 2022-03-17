import { MulticastDelegate } from "./multicast";
import * as Math_ from "./../maths/maths";
import * as Range_ from "./../maths/range";
import Range = Math_.Range;

/** Pattern types */
export enum EPattern
{
	Substring,
	Wildcard,
	RegularExpression
}

/** String matching pattern */
export interface IPattern
{
	/** Returns true if the pattern is active */
	active: boolean;

	/** True if the pattern is a regular expression, false if it's just a substring */
	patn_type: EPattern;

	/** The pattern to use when matching */
	expr: string;

	/** True if the pattern should ignore case */
	ignore_case: boolean;

	/** True if the match result should be inverted */
	invert: boolean;

	/** Only match if the whole line matches */
	whole_line: boolean;

	/** Returns true if the pattern is valid */
	is_valid: boolean;

	/** Raised when the pattern is changed (regenerated due to property changes) */
	pattern_changed: MulticastDelegate<IPattern, IPatternChangedArgs>;
}

/** Event args */
export interface IPatternChangedArgs
{
}

/** A handy regex helper that converts wildcard and substring searches into regex's as well */
export class Pattern implements IPattern
{
	constructor()
	constructor(rhs: IPattern)
	constructor(opt: {active?:boolean, patn_type?:EPattern, expr?:string, ignore_case?:boolean, invert?:boolean, whole_line?:boolean})
	constructor(obj: IPattern|any = {} as any)
	{
		// Defaults
		this._patn_type   = EPattern.Substring;
		this._expr        = "";
		this._ignore_case = false;
		this._compiled_patn = new RegExp("");
		this._validation_exception = null;

		this.pattern_changed = new MulticastDelegate();

		// Optional parameters
		let {
			active = true,
			patn_type = EPattern.Substring,
			expr = "",
			ignore_case = false,
			invert = false,
			whole_line = false
		} = obj;

		this.active      = active;
		this.patn_type   = patn_type;
		this.expr        = expr;
		this.ignore_case = ignore_case;
		this.invert      = invert;
		this.whole_line  = whole_line;
	}

	/** Raised when the pattern is changed (regenerated due to property changes) */
	public pattern_changed: MulticastDelegate<IPattern, IPatternChangedArgs>;

	/** True if the pattern is active */
	public active: boolean;

	/** The type of pattern in 'expr'*/
	get patn_type(): EPattern
	{
		return this._patn_type;
	}
	set patn_type(v: EPattern)
	{
		this._patn_type = v;
		this.InvalidatePatn();
	}
	private _patn_type: EPattern;

	/** The pattern to use when matching */
	get expr(): string
	{
		return this._expr;
	}
	set expr(v:string)
	{
		this._expr = v;
		this.InvalidatePatn();
	}
	private _expr: string;

	/** True if the pattern should ignore case */
	get ignore_case(): boolean
	{
		return this._ignore_case;
	}
	set ignore_case(v: boolean)
	{
		this._ignore_case = v;
		this.InvalidatePatn();
	}
	private _ignore_case: boolean;

	/** True if the match result should be inverted */
	public invert: boolean;

	/** Only match if the whole line matches */
	public whole_line: boolean;

	/** Allows derived patterns to optionally keep whitespace in Substring/wildcard patterns */
	get preserve_whitespace(): boolean
	{
		return false;
	}

	/** Returns true if the pattern is valid */
	get is_valid(): boolean
	{
		return this.ValidateExpr() == null;
	}

	/** Update a property value */
	private InvalidatePatn(): void
	{
		this._compiled_patn = null;
		this._validation_exception = null;
		this.pattern_changed.invoke(this, {});
	}

	/** Returns the match template as a compiled regular expression */
	get regex(): RegExp|null
	{
		if (this._compiled_patn != null)
			return this._compiled_patn;

		// Notes:
		//  If an expression can't be represented in substring, wildcard form, harden up and use a regex

		// Convert the match string into a regular expression string and
		// replace the capture group tags with regex capture groups
		let expr = this.expr;

		// If the expression is a regex, expect regex capture group syntax
		// Otherwise, expect capture groups of the form: {tag}
		if (this.patn_type != EPattern.RegularExpression)
		{
			// Collapse all whitespace to a single space character
			if (!this.preserve_whitespace)
				expr = expr.replace(/\s+/, " ");

			// Escape the regex special chars
			expr = expr.replace(/[-[\]/{}()*+?.\\^$|]/g, '\\$&');

			// Replace wildcards with Regex equivalents
			if (this.patn_type == EPattern.Wildcard)
				expr = expr.replace("\\*", "(.*)").replace("\\?", "(.)");

			// Replace the (now escaped) '{tag}' capture
			// groups with named regular expression capture groups
			expr = expr.replace("\\\\{(\\w+)}", "(?<$1>.*)");

			// Allow expressions that end with whitespace to also match the eol char
			if (this.preserve_whitespace)
			{
				if (/\\ $/.test(expr))
				{
					expr = expr.substr(0, expr.length - 2);
					expr = expr + "(?:$|\\s)";
				}
			}
			else
			{
				// Replace all escaped whitespace with '\s+'
				expr = expr.replace("\\ ", "\\s+");

				// Allow expressions that end with whitespace to also match the eol char
				if (/\\s+$/.test(expr))
				{
					expr = expr.substr(0, expr.length - 3);
					expr = expr + "(?:$|\\s)";
				}
			}
		}

		// Compile the expression
		var flags = (this.ignore_case ? "i" : "") + "g";
		return this._compiled_patn = new RegExp(expr, flags);
	}
	private _compiled_patn: RegExp|null;

	/** Return the compiled regex string for reference */
	get regex_string(): string
	{
		var ex = this.ValidateExpr();
		if (ex != null) return "Expression invalid - " + ex.message;
		if (this.regex == null) return "";
		return this.regex.toString();
	}

	/** Returns null if the match field is valid, otherwise an exception describing what's wrong */
	ValidateExpr() :Error|null
	{
		try
		{
			// Already have a validate exception, return it
			if (this._validation_exception != null)
				return this._validation_exception;

			// Compiling the Regex will throw if there's something wrong with it.
			if (this.regex == null)
				return this._validation_exception = new SyntaxError("The regular expression is null");

			// No prob, bob!
			return this._validation_exception = null;
		}
		catch (ex)
		{
			let err : Error;
			if (ex instanceof Error) err = ex; else err = Error(String(ex));
			return this._validation_exception = err;
		}
	}
	private _validation_exception: Error|null;

	/** Returns a string describing what's wrong with the expression */
	get SyntaxErrorDescription(): string
	{
		var ex = this.ValidateExpr();
		return ex == null ? "" : ex.message;
	}

	/** Returns true if this pattern matches a substring in 'text' */
	public IsMatch(text: string): boolean
	{
		if (!this.active || !this.is_valid || this.regex == null)
			return false;

		this.regex.lastIndex = 0;
		let m = this.regex.exec(text);
		let matched = m != null && (!this.whole_line || m[0] == m.input);
		return this.invert ? !matched : matched;
	}

	/** Returns the capture groups captured when applying this pattern to 'text' */
	public Match(text:string): string[]
	{
		if (!this.active || !this.is_valid || this.regex == null)
			return [];

		this.regex.lastIndex = 0;
		let m = this.regex.exec(text);
		return m != null && (!this.whole_line || m[0] == m.input) ? m : [];
	}

	/**
	 * Return index ranges within text' that match this pattern.
	 * Note, the first returned span will be the string that matches the entire pattern.
	 * Any subsequent strings will be the capture groups from within the regex pattern.
	 * i.e.
	 *  if your expression is "x" you get one group when matched on "xox" equal to [0,1]
	 *  if your expression is "(x)" you get two groups, [0,1] for the whole expression, then [0,1] for the sub-expression
	 * Note, only the first match is returned, [2,1] is not returned by this method.
	 */
	MatchRanges(text:string, start:number = 0, length:number = -1): Range[]
	{
		if (!this.active || text == null || this.regex == null)
			return [];

		let x:number[] = [];

		if (this.invert) x.push(0);
		try
		{
			text = text.substr(start, length == -1 ? text.length - start : length);

			this.regex.lastIndex = 0;
			let m = this.regex.exec(text);
			for (;m != null && (!this.whole_line || m[0] == m.input);)
			{
				x.push(m.index);
				x.push(m.index + m[0].length);

				m = this.regex.exec(text);
			}
		}
		catch (ArgumentException) {}
		if (this.invert) x.push(text.length);

		let ranges:Range[] = [];
		for (let i = 0; i != x.length; i += 2)
			ranges.push(Range_.create(x[i], x[i+1]));

		return ranges;
	}

	// /**
	// * Returns all occurrences of matches within 'text'.
	// * i.e.
	// *   AllMatches(@"(x)", "xoxox") returns [0,1], [2,1], [4,1]
	// * Note, this method doesn't return capture groups, only whole expression matches.
	// */
	// AllMatches(text:string): Range[]
	// {
	// 	var ranges = [];
	// 	for (var i = 0;;)
	// 	{
	// 		var span = Match(text,i).FirstOrDefault();
	// 		if (span.Size == 0) yield break;
	// 		yield return span;
	// 		i = (int)span.End;
	// 	}
	// }

	// 	/// <summary>Creates a new object that is a copy of the current instance.</summary>
	// 	public virtual object Clone()
	// 	{
	// 		return new Pattern(this);
	// 	}

	// 	/// <summary>Expression</summary>
	// 	public override string ToString()
	// 	{
	// 		return $"{PatnType}: {Expr}";
	// 	}

	// 	#region Equals
	// 	public static bool operator == (Pattern lhs, Pattern rhs)
	// 	{
	// 		return ReferenceEquals(lhs,rhs) || Equals(lhs, rhs);
	// 	}
	// 	public static bool operator != (Pattern lhs, Pattern rhs)
	// 	{
	// 		return !(lhs == rhs);
	// 	}
	// 	public bool Equals(Pattern rhs)
	// 	{
	// 		return
	// 			rhs != null &&
	// 			m_patn_type     == rhs.m_patn_type   && 
	// 			m_expr          == rhs.m_expr        && 
	// 			m_ignore_case   == rhs.m_ignore_case && 
	// 			m_active        == rhs.m_active      && 
	// 			m_invert        == rhs.m_invert      && 
	// 			m_whole_line    == rhs.m_whole_line; 
	// 	}
	// 	public override bool Equals(object obj)
	// 	{
	// 		return Equals(obj as Pattern);
	// 	}
	// 	public override int GetHashCode()
	// 	{
	// 		return
	// 			m_patn_type   .GetHashCode()^
	// 			m_expr        .GetHashCode()^
	// 			m_ignore_case .GetHashCode()^
	// 			m_active      .GetHashCode()^
	// 			m_invert      .GetHashCode()^
	// 			m_whole_line  .GetHashCode();
	// 	}
	// 	#endregion
	// }
	// private static class XmlTag
	// 	{
	// 		public const string Expr       = "expr";
	// 		public const string PatnType   = "patntype";
	// 		public const string Active     = "active";
	// 		public const string IgnoreCase = "ignorecase";
	// 		public const string Invert     = "invert";
	// 		public const string WholeLine  = "wholeline";
	// 	}
}
