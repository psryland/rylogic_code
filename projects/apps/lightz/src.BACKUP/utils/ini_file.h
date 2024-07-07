// INI File Expected format:
// Lines that do not start with one of these formats are ignored
//   ; ....   -> comment line
//   [Section] -> Section start definition
//   <identifier> = <value> ... -> key/value pair
#pragma once
#include "forward.h"

namespace lightz::ini_file
{
	#if 0
	enum class EElement
	{
		EoF,
		Section,
		KeyValue,
	};

	class Iterator
	{
		Stream& m_text;
		String m_line;
		EElement m_type;
		char const* m_kbeg;
		char const* m_kend;
		char const* m_vbeg;
		char const* m_vend;
		int m_line_no;

	public:

		Iterator(Stream& text)
			: m_text(text)
			, m_line()
			, m_type()
			, m_kbeg()
			, m_kend()
			, m_vbeg()
			, m_vend()
			, m_line_no(1)
		{
			Next();
		}
		Iterator(Iterator&&) = default;
		Iterator(Iterator const&) = delete;
		Iterator& operator =(Iterator&&) = default;
		Iterator& operator =(Iterator const&) = delete;

		// Get the current line number
		int LineNo() const
		{
			return m_line_no;
		}

		// Get the value for the current key/value pair
		std::string_view Value() const
		{
			return {m_vbeg, size_t(m_vend - m_vbeg)};
		}

		// Match the key name against the given string
		bool IsMatch(EElement type) const
		{
			return m_type == type;
		}
		bool IsMatch(EElement type, char const* name) const
		{
			return m_type == type && lwip_strnicmp(name, m_kbeg, m_kend - m_kbeg) == 0;
		}

		// True when the ini stream is exhausted
		bool AtEnd() const
		{
			return m_type == EElement::EoF;
		}

		// Read the next line from the ini stream
		void Next()
		{
			// Adjust 'beg' and 'end' to remove leading and trailing whitespace
			static auto TrimWS = [](char const*& beg, char const*& end)
			{
				for (; beg != end && isspace(*beg); ++beg) {}
				for (; end != beg && isspace(end[-1]); --end) {}
			};

			for (; m_text.available() != 0; ++m_line_no)
			{
				m_line = m_text.readStringUntil('\n');

				// Blank line
				if (m_line.isEmpty())
					continue;

				// Comment
				if (m_line[0] == ';')
					continue;

				// Section
				if (m_line[0] == '[')
				{
					auto end = m_line.indexOf(']');
					if (end == -1)
						continue;

					m_kbeg = &m_line[0] + 1;
					m_kend = &m_line[0] + end;
					TrimWS(m_kbeg, m_kend);

					m_vbeg = &m_line[0];
					m_vend = &m_line[0];
					TrimWS(m_vbeg, m_vend);

					m_type = EElement::Section;
					return;
				}

				// Key/Value pair
				else
				{
					auto eq = m_line.indexOf('=');
					if (eq == -1)
						continue;

					m_kbeg = &m_line[0] + 0;
					m_kend = &m_line[0] + eq;
					TrimWS(m_kbeg, m_kend);

					m_vbeg = &m_line[0] + eq + 1;
					m_vend = &m_line[0] + m_line.length();
					TrimWS(m_vbeg, m_vend);

					m_type = EElement::KeyValue;
					return;
				}
			}
			m_kbeg = m_kend = nullptr;
			m_vbeg = m_vend = nullptr;
			m_type = EElement::EoF;
		}
		Iterator& operator ++()
		{
			Next();
			return *this;
		}
	};
	#endif
}
