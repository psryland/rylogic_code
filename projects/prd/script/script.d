module prd.script.script;

import std.algorithm;

/// A line,column location within a stream
struct Loc
{
	string m_file;
	size_t m_line;
	size_t m_col;
	
	this(string file, size_t line, size_t col)
	{
		m_file = file;
		m_line = line;
		m_col = col;
	}
	
	char adv(char c)
	{
		if      (c == '\n') { ++m_line; m_col = 0; }
		else if (c != 0)    { ++m_col; }
		return c;
	}
	
	int opCmp(ref const Loc rhs)
	{
		if (m_file != rhs.m_file) return cmp(m_file, rhs.m_file);
		if (m_line != rhs.m_line) return cast(int)(m_line - rhs.m_line);
		return cast(int)(m_col - rhs.m_col);
	}
}
unittest
{
	Loc loc = Loc("file", 0, 0);
	loc.adv('1');
	loc.adv('\n');
	loc.adv('\n');
	loc.adv('2');
	assert(loc.m_line == 2);
	assert(loc.m_col == 1);

	Loc loc2 = loc;
	assert(loc2 == loc);

	loc.adv('a');
	assert(loc2 != loc);
	assert(loc2 < loc);
}

/// A source of characters
abstract class Src
{
protected:
	abstract char peek() const;
	abstract void next();
	void seek() {}
}

/// Strips comments from a 'Src'
class CommentStrip :Src
{
	protected override char peek() const { return 0; }
	protected override void next() { }
}
