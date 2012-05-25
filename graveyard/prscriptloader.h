//*************************************************************
//
// ScriptLoader
//
//*************************************************************

#ifndef PRSCRIPTLOADER_H
#define PRSCRIPTLOADER_H

// Don't include this directly. Include PRScript.h
#include "pr/common/PRScript.h"

namespace pr
{
	namespace impl
	{
		template <typename T>
		class ScriptLoader
		{
		public:
			ScriptLoader();
			explicit ScriptLoader(const char* filename);
			explicit ScriptLoader(const char* string, std::size_t length);
			void	Reset();
			script::EResult	LoadFromFile(const char* filename);
			script::EResult	LoadFromString(const char* string, std::size_t length);
			bool	IsLoaded() const { return !m_source.empty(); }
			void	ThrowExceptions(bool yes_or_no) { m_throw = yes_or_no; }
			void	IgnoreMissingIncludes(bool yes_or_no) { m_ignore_missing_includes = yes_or_no; }
			void	ClearIncludePaths() { m_include_paths.clear(); }
			void	AddIncludePath(std::string const& path);
			pr::script::TPaths const& GetIncludedFiles() const { return m_included_files; }

			// Use for iterating
			bool	GetKeyword(std::string& keyword) { return GetKeyword(keyword, 0); }
			bool	GetKeyword(std::string& keyword, unsigned int* from);
			bool	PeekKeyword(std::string& keyword) const { return PeekKeyword(keyword, 0); }
			bool	PeekKeyword(std::string& keyword, unsigned int* from) const;
			
			// Use to find a specific keyword
			bool	FindKeyword(const char* keyword) { return FindKeyword(keyword, 0); }
			bool	FindKeyword(const char* keyword, std::size_t* from);
			
			bool	FindSectionStart();
			bool	FindSectionEnd();
			bool	FindNextLine();
			script::EResult	GetSection(const std::string& section_name, ScriptLoader<T>& sub_section) { return GetSection(section_name, 0, sub_section); }
			script::EResult	GetSection(const std::string& section_name, std::size_t* from, ScriptLoader<T>& sub_section);
			
			std::string CopySection() const;
			bool	ExtractString(std::string& words);
			bool	ExtractCString(std::string& words);
			bool	ExtractIdentifier(std::string& word);
			bool	ExtractByte(uint8& uint8_, int radix);
			bool	ExtractLong(long& long_, int radix);
			bool	ExtractLongArray(long* long_, std::size_t count, int radix);
			bool	ExtractInt(int& int_, int radix);
			bool	ExtractIntArray(int* int_, std::size_t count, int radix);
			bool	ExtractULong(unsigned long& ulong_, int radix);
			bool	ExtractULongArray(unsigned long* ulong_, std::size_t count, int radix);
			bool	ExtractUInt(unsigned int& uint_, int radix);
			bool	ExtractUIntArray(unsigned int* uint_, std::size_t count, int radix);
			bool	ExtractFloat(float& float_);
			bool	ExtractFloatArray(float* float_, std::size_t count);
			bool	ExtractDouble(double& double_);
			bool	ExtractBool(bool& bool_);
			bool	ExtractVector3(v4& vector, float w);
			bool	ExtractVector4(v4& vector);
			bool	ExtractQuaternion(Quat& quaternion);
			bool	Extractm4x4(m4x4& transform);
			bool	ExtractBinary(void* data, unsigned int size);
			void	SetPosition(unsigned int pos)							{ PR_ASSERT(PR_DBG_COMMON, pos < GetDataLength()); m_pos = m_first + pos; }
			void	SetDelimiters(const std::string& delimiters)			{ m_delimiters = delimiters; }
			bool	IsKeyword()												{ SkipWhiteSpace(); return *m_pos == m_keyword_identifier; }
			bool	IsSectionStart()										{ SkipWhiteSpace(); return *m_pos == m_section_start; }
			bool	IsSectionEnd()											{ SkipWhiteSpace(); return *m_pos == m_section_end; }
			std::size_t GetDataLength() const								{ return m_source.size(); }
			std::size_t GetPosition() const									{ return m_pos - m_first; }
			const char* GetFilename() const									{ return m_filename.c_str(); }
			const char* GetSourceString() const								{ return m_first; }
			const char* GetSourceStringAt() const							{ return m_pos; }
			const char* GetSourceStringAt(unsigned int pos) const			{ return m_first + pos; }

		private:
			enum { LineComment, BlockComment };
			bool	PeekKeyword(std::string& keyword, const char*& pos) const;
			bool	SkipSection(const char*& pos) const;
			bool	IsCommentStart(const char* str, unsigned int& comment_type) const;
			bool	IsCommentEnd  (const char* str, unsigned int  comment_type) const;
			bool	IsInclude(const char* str) const;
			void	SkipComment(unsigned int comment_type, const char*& str, const char* str_end);
			void	PreProcess();
			void	InsertIncludeFile(const char* str, const char* include_filename);
			bool	IsDelimiter(char ch) const;
			bool	IsControlChar(char ch) const;
			void	SkipWhiteSpace();

		private:
			std::string			m_filename;
			std::string			m_source;
			const char* 		m_first;
			const char* 		m_last;
			const char* 		m_pos;
			char				m_keyword_identifier;
			char				m_line_comment[2];
			char				m_block_comment_start[2];
			char				m_block_comment_end[2];
			char				m_section_start;
			char				m_section_end;
			bool				m_throw;
			bool				m_ignore_missing_includes;
			std::string			m_delimiters;
			std::string			m_include_kw;
			pr::script::TPaths	m_include_paths;
			pr::script::TPaths	m_included_files;
		};

		// Constructor
		template <typename T>
		ScriptLoader<T>::ScriptLoader()
		:m_filename					("")
		,m_source					("\0")
		,m_first					(&m_source[0])
		,m_last						(m_first)
		,m_pos						(m_first)
		,m_keyword_identifier		('*')
		,m_section_start			('{')
		,m_section_end				('}')
		,m_throw					(true)
		,m_ignore_missing_includes	(false)
		,m_delimiters				(" ;,")
		,m_include_kw				("#include")
		{
			m_line_comment[0]		= '/'; m_line_comment[1]		= '/'; 
			m_block_comment_start[0]= '/'; m_block_comment_start[1]	= '*';
			m_block_comment_end[0]  = '*'; m_block_comment_end[1]	= '/';
		}
		template <typename T>
		ScriptLoader<T>::ScriptLoader(const char* filename)
		{
			new (this) ScriptLoader();
			if( filename )
			{
				script::EResult result = LoadFromFile(filename);
				if( Failed(result) ) { if( m_throw ) throw script::Exception(result, "Failed to load source script"); } 
			}
		}
		template <typename T>
		ScriptLoader<T>::ScriptLoader(const char* string, std::size_t length)
		{
			new (this) ScriptLoader();
			if( string && length != 0 )
			{
				script::EResult result = LoadFromString(string, length);
				if( Failed(result) ) { if( m_throw ) throw script::Exception(result, "Failed to load source script"); } 
			}
		}

		// Clear any previously loaded data
		template <typename T>
		void ScriptLoader<T>::Reset()
		{
			m_source.resize(0);
			m_filename = "";
			m_included_files.clear();
		}

		// Load the file
		template <typename T>
		script::EResult ScriptLoader<T>::LoadFromFile(const char* filename)
		{
			Reset();
			m_filename = filename;
			AddIncludePath(pr::filesys::GetDirectory(pr::filesys::GetFullPath(m_filename)));
			if( !FileToBuffer(filename, m_source) || m_source.empty() )	{ return script::EResult_LoadSourceFailed; }
			try								{ PreProcess(); }
			catch( const pr::Exception& e )	{ return static_cast<script::EResult>(e.m_value); }

			m_first = &m_source[0];
			m_last  = m_first + m_source.size();
			m_pos   = m_first;
			return script::EResult_Success;
		}

		// Load the source data from a string
		template <typename T>
		script::EResult ScriptLoader<T>::LoadFromString(const char* string, std::size_t length)
		{
			Reset();
			m_filename = "";
			m_source.resize(length);
			std::copy(string, string + length, &m_source[0]);
			try								{ PreProcess(); }
			catch( const pr::Exception& e )	{ return static_cast<script::EResult>(e.m_value); }

			m_first = &m_source[0];
			m_last  = m_first + m_source.size();
			m_pos   = m_first;
			return script::EResult_Success;
		}

		// Add a path to the include paths
		template <typename T>
		void ScriptLoader<T>::AddIncludePath(std::string const& path)
		{
			std::string path_str = pr::filesys::Standardise(path);
			pr::script::TPaths::iterator iter = std::find(m_include_paths.begin(), m_include_paths.end(), path_str);
			if( iter == m_include_paths.end() )
			{
				m_include_paths.push_back(path_str); // Note, these paths have no trailing '\'
			}
		}

		// Searches for the next keyword in the source data.
		// If found 'm_pos' is moved to one past the keyword.
		// If 'from' is provided, searching begins from 'from' otherwise from the internal 'm_pos' position.
		template <typename T>
		bool ScriptLoader<T>::GetKeyword(std::string& keyword, unsigned int* from)
		{
			if( from ) m_pos = Clamp<const char*>(m_first + *from, m_first, m_last - 1);
			return PeekKeyword(keyword, m_pos);
		}

		// Searches for the next keyword in the source data.
		// 'm_pos' is not modified by this call
		// If 'from' is provided, searching begins from 'from' otherwise from the internal 'm_pos' position.
		template <typename T>
		bool ScriptLoader<T>::PeekKeyword(std::string& keyword, unsigned int* from) const
		{
			const char* pos = m_pos;
			if( from ) pos = Clamp<const char*>(m_first + *from, m_first, m_last - 1);
			return PeekKeyword(keyword, pos);
		}

		// Searches for 'keyword' in the source data.
		// If found 'm_pos' is moved to one past the keyword.
		// If 'from' is provided, searching begins from 'from' otherwise from the internal 'm_pos' position.
		template <typename T>
		bool ScriptLoader<T>::FindKeyword(const char* keyword, std::size_t* from)
		{
			if( from ) m_pos = Clamp<const char*>(m_first + *from, m_first, m_last - 1);
			std::string kw;
			while( GetKeyword(kw) )
			{
				if( str::EqualNoCase(keyword, kw) ) return true;
			}
			return false;
		}

		// Moves 'm_pos' to one past the 'm_section_start' character
		template <typename T>
		bool ScriptLoader<T>::FindSectionStart()
		{
			while( m_pos != m_last )
			{
				if( *m_pos == m_section_start ) { ++m_pos; return true;  }
				if( *m_pos == m_section_end   ) { ++m_pos; if( m_throw ) { throw script::Exception(script::EResult_SectionStartNotFound); } else { return false; } }
				if( ++m_pos == m_last )			{		   if( m_throw ) { throw script::Exception(script::EResult_SectionStartNotFound); } else { return false; } }
			}
			if( m_throw ) { throw script::Exception(script::EResult_SectionStartNotFound); }
			return false;
		}

		// Moves 'm_pos' to one past the 'm_section_end' character
		template <typename T>
		bool ScriptLoader<T>::FindSectionEnd()
		{
			while( m_pos != m_last )
			{
				if( *m_pos == m_section_end )	{ ++m_pos; return true; }
				if( ++m_pos == m_last )			{ if( m_throw ) { throw script::Exception(script::EResult_SectionEndNotFound); } else { return false; } }
			}
			if( m_throw ) { throw script::Exception(script::EResult_SectionEndNotFound); }
			return false;
		}

		//*****
		// Moves 'm_pos' to the start of the next line
		template <typename T>
		bool ScriptLoader<T>::FindNextLine()
		{
			while( m_pos != m_last && (*m_pos != '\r' && *m_pos != '\n') ) { ++m_pos; }
			if( m_pos == m_last ) return false;
			while( m_pos != m_last && (*m_pos == '\r' || *m_pos == '\n') ) { ++m_pos; }
			if( m_pos == m_last ) return false;
			return true;
		}

		//*****
		// Copy a section within a script into a new scriptloader
		template <typename T>
		script::EResult	ScriptLoader<T>::GetSection(const std::string& section_name, std::size_t* from, ScriptLoader<T>& sub_section)
		{
			if( !FindKeyword(section_name.c_str(), from) )	{ if( m_throw ) { throw script::Exception(script::EResult_SectionNotFound); } else { return script::EResult_SectionNotFound; } }
			if( !FindSectionStart() )						{ if( m_throw ) { throw script::Exception(script::EResult_NotASection); } else { return script::EResult_NotASection; } }
			std::string section = CopySection();
			sub_section.LoadFromString(section.c_str(), section.size());
			if( !FindSectionEnd() )							{ if( m_throw ) { throw script::Exception(script::EResult_NotASection); } else { return script::EResult_NotASection; } }
			if( from ) *from = GetPosition();
			return script::EResult_Success;
		}
        
		//*****
		// Returns a string containing everything from 'm_pos' to the
		// next 'm_section_end' or the end of the data
		template <typename T>
		std::string ScriptLoader<T>::CopySection() const
		{
			unsigned int nest = 1;
			const char* pos = m_pos;
			while( pos != m_last )
			{
				if( *pos == m_section_end )
				{
					--nest;
					if( nest == 0 ) break;
				}
				if( *pos == m_section_start )
				{
					++nest;
				}
				++pos;
			}
			std::string copy(m_pos, pos);
			return copy;
		}

		// Extracts characters between '"'
		template <typename T>
		bool ScriptLoader<T>::ExtractString(std::string& words)
		{
			SkipWhiteSpace();
			
			std::string word = "";
			if( *m_pos != '"' ) { if( m_throw ) { throw script::Exception(script::EResult_ExtractString); } else { return false; } }
			++m_pos;
			while( m_pos != m_last && *m_pos != '"' )
			{
				word += *m_pos;
				if( ++m_pos == m_last ) { if( m_throw ) { throw script::Exception(script::EResult_ExtractString); } else { return false; } }
			}
			++m_pos;
			words = word;
			return true;
		}

		// Extracts characters between '"'
		template <typename T>
		bool ScriptLoader<T>::ExtractCString(std::string& words)
		{
			SkipWhiteSpace();
			
			std::string word = "";
			if( *m_pos != '"' ) { if( m_throw ) { throw script::Exception(script::EResult_ExtractCString); } else { return false; } }
			++m_pos;
			while( m_pos != m_last && *m_pos != '"' )
			{
				if( *m_pos == '\\' )
				{
					if( ++m_pos == m_last ) { if( m_throw ) { throw script::Exception(script::EResult_ExtractCString); } else { return false; } }
					switch( *m_pos )
					{
					case 'a':	word += "\a"; break;
					case 'b':	word += "\b"; break;
					case 'f':	word += "\f"; break;
					case 'n':	word += "\n"; break;
					case 'r':	word += "\r"; break;
					case 't':	word += "\t"; break;
					case 'v':	word += "\v"; break;
					case '\\':	word += "\\"; break;
					case '?':	word += "\?"; break;
					case '\'':	word += "\'"; break;
					case '"':	word += "\""; break;
					default: PR_INFO(PR_DBG_COMMON, "Unknown escape character");
					};
				}
				else
				{
					word += *m_pos;
				}

				if( ++m_pos == m_last ) { if( m_throw ) { throw script::Exception(script::EResult_ExtractCString); } else { return false; } }
			}
			++m_pos;
			words = word;
			return true;
		}

		// Extracts a block of non-white space characters
		template <typename T>
		bool ScriptLoader<T>::ExtractIdentifier(std::string& word)
		{
			SkipWhiteSpace();

			std::string id = "";
			while( m_pos != m_last && !IsDelimiter(*m_pos) && !IsControlChar(*m_pos) )
			{
				id += *m_pos;
				if( ++m_pos == m_last ) { if( m_throw ) { throw script::Exception(script::EResult_ExtractIdentifier); } else { return false; } }
			}
			word = id;
			return true;
		}

		// Read a uint8 from the source data
		template <typename T>
		bool ScriptLoader<T>::ExtractByte(uint8& uint8_, int radix)
		{
			unsigned int i;
			if( !ExtractUInt(i, radix) ) { return false; }
			uint8_ = static_cast<uint8>(i);
			return true;
		}

		// Read a long from the source data
		template <typename T>
		bool ScriptLoader<T>::ExtractLong(long& long_, int radix)
		{
			SkipWhiteSpace();
			
			std::string long_str;
			while( m_pos != m_last && !IsDelimiter(*m_pos) && !IsControlChar(*m_pos) )
			{
				if( radix == 10 && !isdigit (*m_pos) && *m_pos != '-' ) break;
				if( radix == 16 && !isxdigit(*m_pos) && *m_pos != '-' ) break;

				long_str += *m_pos;
				if( ++m_pos == m_last ) { if( m_throw ) { throw script::Exception(script::EResult_ExtractLong); } else { return false; } }
			}
			if( long_str.empty() ) { if( m_throw ) { throw script::Exception(script::EResult_ExtractLong); } else { return false; } }
			long_ = strtol(long_str.c_str(), 0, radix);
			return true;
		}
		
		// Read an array of Long's from the source data
		template <typename T>
		bool ScriptLoader<T>::ExtractLongArray(long* long_, std::size_t count, int radix)
		{
			while( count-- )
				if( !ExtractLong(*long_++, radix) ) { return false; }
			return true;
		}

		// Read a unsigned int from the source data
		template <typename T>
		bool ScriptLoader<T>::ExtractInt(int& int_, int radix)
		{
			long long_;
			if( !ExtractLong(long_, radix) ) { return false; }
			int_ = static_cast<int>(long_);
			return true;
		}

		// Read an array of Uint's from the source data
		template <typename T>
		bool ScriptLoader<T>::ExtractIntArray(int* int_, std::size_t count, int radix)
		{
			while( count-- )
				if( !ExtractInt(*int_++, radix) ) { return false; }
			return true;
		}

		// Read a ulong from the source data
		template <typename T>
		bool ScriptLoader<T>::ExtractULong(unsigned long& ulong_, int radix)
		{
			SkipWhiteSpace();
			
			std::string ulong_str;
			while( m_pos != m_last && !IsDelimiter(*m_pos) && !IsControlChar(*m_pos) )
			{
				if( radix == 10 && !isdigit (*m_pos) ) break;
				if( radix == 16 && !isxdigit(*m_pos) ) break;

				ulong_str += *m_pos;
				if( ++m_pos == m_last ) { if( m_throw ) { throw script::Exception(script::EResult_ExtractULong); } else { return false; } }
			}
			if( ulong_str.empty() ) { if( m_throw ) { throw script::Exception(script::EResult_ExtractULong); } else { return false; } }
			ulong_ = strtoul(ulong_str.c_str(), 0, radix);
			return true;
		}

		// Read an array of ulong's from the source data
		template <typename T>
		bool ScriptLoader<T>::ExtractULongArray(unsigned long* ulong_, std::size_t count, int radix)
		{
			while( count-- )
				if( !ExtractULong(*ulong_++, radix) ) { return false; }
			return true;
		}

		// Read a unsigned int from the source data
		template <typename T>
		bool ScriptLoader<T>::ExtractUInt(unsigned int& uint_, int radix)
		{
			unsigned long ulong_;
			if( !ExtractULong(ulong_, radix) ) { return false; }
			uint_ = static_cast<unsigned int>(ulong_);
			return true;
		}

		// Read an array of Uint's from the source data
		template <typename T>
		bool ScriptLoader<T>::ExtractUIntArray(unsigned int* uint_, std::size_t count, int radix)
		{
			while( count-- )
				if( !ExtractUInt(*uint_++, radix) ) { return false; }
			return true;
		}

		// Read a float from the source data
		template <typename T>
		bool ScriptLoader<T>::ExtractFloat(float& float_)
		{
			double d;
			if( !ExtractDouble(d) ) { return false; }
			float_ = static_cast<float>(d);
			return true;
		}
		template <typename T>
		bool ScriptLoader<T>::ExtractFloatArray(float* float_, std::size_t count)
		{
			while( count-- )
				if( !ExtractFloat(*float_++) ) { return false; }
			return true;
		}

		//*****
		// Read a double from the source data
		template <typename T>
		bool ScriptLoader<T>::ExtractDouble(double& double_)
		{
			SkipWhiteSpace();
			
			std::string double_str;
			while( m_pos != m_last && !IsDelimiter(*m_pos) && !IsControlChar(*m_pos) )
			{
				if( !isdigit(*m_pos) &&
					*m_pos != '.' &&
					*m_pos != '-' &&
					*m_pos != 'e' &&
					*m_pos != 'E') break;

				double_str += *m_pos;
				if( ++m_pos == m_last ) { if( m_throw ) { throw script::Exception(script::EResult_ExtractReal); } else { return false; } }
			}
			if( double_str.length() == 0 ) { if( m_throw ) { throw script::Exception(script::EResult_ExtractReal); } else { return false; } }
			double_ = strtod(double_str.c_str(), 0);
			return true;
		}

		//*****
		// Read a boolean from the source data
		template <typename T>
		bool ScriptLoader<T>::ExtractBool(bool& bool_)
		{
			unsigned int i;
			if( ExtractUInt(i, 10) ) { bool_ = i != 0; return true; } 
			return false;
		}

		//*****
		// Read a 3 component vector from the source data
		template <typename T>
		bool ScriptLoader<T>::ExtractVector3(v4& vector, float w)
		{
			float x,y,z;
			if( !(ExtractFloat(x) && ExtractFloat(y) && ExtractFloat(z)) ) { return false; }
			vector.set(x,y,z,w);
			return true;
		}

		//*****
		// Read a 4 component vector from the source data
		template <typename T>
		bool ScriptLoader<T>::ExtractVector4(v4& vector)
		{
			float x,y,z,w;
			if( !(ExtractFloat(x) && ExtractFloat(y) && ExtractFloat(z) && ExtractFloat(w)) ) { return false; }
			vector.set(x,y,z,w);
			return true;
		}

		//*****
		// Read a quaternion from the source data
		template <typename T>
		bool ScriptLoader<T>::ExtractQuaternion(Quat& quaternion)
		{
			float x,y,z,w;
			if( !(ExtractFloat(x) && ExtractFloat(y) && ExtractFloat(z) && ExtractFloat(w)) ) { return false; }
			quaternion.set(x,y,z,w);
			return true;
		}

		//*****
		// Read a matrix from the source data
		template <typename T>
		bool ScriptLoader<T>::Extractm4x4(m4x4& transform)
		{
			m4x4 txfm = m4x4Identity;
			for( unsigned int j = 0; j < 4; ++j )
				for( unsigned int i = 0; i < 4; ++i )
					if( !ExtractFloat(txfm[j][i]) ) { return false; }
			transform = txfm;
			return true;
		}

		//*****
		// Read binary data from the source data
		template <typename T>
		bool ScriptLoader<T>::ExtractBinary(void* data, unsigned int size)
		{
			TBinaryData buffer(size);
			uint8* ptr = &buffer[0];
			for( unsigned int b = 0; b < size; ++b )
			{
				if( !ExtractByte(*ptr, 16) ) { return false; }
				++ptr;
			}
			std::copy(buffer.begin(), buffer.end(), static_cast<uint8*>(data));
			return true;
		}

		//*****
		// Scans from 'pos', to the first m_keyword_identifier char or end of data
		// If an 'm_section_start' char is encountered then scanning skips everything
		// until a 'm_section_end' is found
		template <typename T>
		bool ScriptLoader<T>::PeekKeyword(std::string& keyword, const char*& pos) const
		{
			while( pos != m_last )
			{
				if( *pos == m_keyword_identifier )
				{
					++pos;
					keyword = "";
					while( pos != m_last && !IsDelimiter(*pos) && !IsControlChar(*pos) )
					{
						keyword += *pos;
						if( ++pos == m_last ) return false;
					}
					return true;
				}

				if( *pos == m_section_start )
				{
					if( !SkipSection(pos) ) return false;
				}

				if( *pos == m_section_end ) return false;
				if( ++pos == m_last ) return false;
			}
			return false;
		}

		//*****
		// Skip over a 'm_section_start' -> 'm_section_end' section
		template <typename T>
		bool ScriptLoader<T>::SkipSection(const char*& pos) const
		{
			PR_ASSERT(PR_DBG_COMMON, *pos == m_section_start);
			int nest = 0;
			do
			{
				nest += *pos == m_section_start;
				nest -= *pos == m_section_end;
			}
			while( ++pos != m_last && nest != 0 );
			if( pos == m_last ) return false;
			++pos;
			return true;
		}

		//*****
		// Returns true if 'str' points to the start of a comment
		template <typename T>
		bool ScriptLoader<T>::IsCommentStart(const char* str, unsigned int& comment_type) const
		{
			if( !str ) return false;
			if( *str == m_line_comment[0]        && *(str + 1) == m_line_comment[1]        ) { comment_type = LineComment; return true; }
			if( *str == m_block_comment_start[0] && *(str + 1) == m_block_comment_start[1] ) { comment_type = BlockComment; return true; }
			return false;
		}
		
		//*****
		// Returns true if 'str' points to the end of a comment
		template <typename T>
		bool ScriptLoader<T>::IsCommentEnd(const char* str, unsigned int comment_type) const
		{
			if( !str ) return false;
			if( comment_type == LineComment  ) return *str == '\n';
			else
			if( comment_type == BlockComment ) return *(str - 1) == m_block_comment_end[0] && *str == m_block_comment_end[1];
			return false;
		}

		//*****
		// Returns true if 'str' is an include file declaration
		template <typename T>
		bool ScriptLoader<T>::IsInclude(const char* str) const
		{
			return _strnicmp(str, m_include_kw.c_str(), m_include_kw.size()) == 0;			
		}

		//*****
		// Skip over a comment.
		template <typename T>
		void ScriptLoader<T>::SkipComment(unsigned int comment_type, const char*& str, const char* str_end)
		{
			while( str != str_end && !IsCommentEnd(str, comment_type) ) { ++str; }
			if( str != str_end ) { ++str; }
		}

		//*****
		// Make a copy of the string passed to us and remove all comments from it.
		// Returns the length of the copied string
		template <typename T>
		void ScriptLoader<T>::PreProcess()
		{
			char* in			= &m_source[0];
			const char* str     = &m_source[0];
			const char* str_end = str + m_source.size();
			unsigned int comment_type = 0;
			while( str != str_end )
			{
				if( IsCommentStart(str, comment_type) )
				{
					SkipComment(comment_type, str, str_end);
				}
				else if( IsInclude(str) )
				{
					std::size_t in_offset  = in  - &m_source[0];
					std::size_t str_offset = str - &m_source[0];
					InsertIncludeFile(str, str_end);
					in		= &m_source[in_offset];
					str		= &m_source[str_offset];
					str_end	= &m_source[0] + m_source.size();
				}
				else
				{
					*in++ = *str++;
				}
			}
			m_source.resize(in - &m_source[0]);
		}

		// Insert the contents of an include file into 'm_source'
		template <typename T>
		void ScriptLoader<T>::InsertIncludeFile(const char* str_start, const char* str_end)
		{
			// Read the include filename
			const char* str = str_start + m_include_kw.size();
			while( str < str_end && *str != '"' ) ++str;
			++str;	// skip the '"'
			if( str > str_end ) throw script::Exception(script::EResult_IncludeFilenameMissing, "Failed to find the filename for an include");

			std::string filename;
			while( str < str_end && *str != '"' ) { filename += *str; ++str; }
			++str;	// skip the '"'
			if( str > str_end ) throw script::Exception(script::EResult_FailedToReadIncludeFilename, "Failed to read the include filename");

			std::size_t insert_offset = str_start - &m_source[0];

			// Remove the include declaration from the source
			m_source.erase(insert_offset, str - str_start);

			// Find the file
			bool file_found = pr::filesys::FileExists(filename);
			if( !file_found )
			{
				for( pr::script::TPaths::const_iterator i = m_include_paths.begin(), i_end = m_include_paths.end(); i != i_end; ++i )
				{
					if( pr::filesys::FileExists(*i + "\\" + filename) )
					{
						filename = *i + "\\" + filename;
						file_found = true;
						break;
					}
				}
			}
			if( !file_found )
			{
				if( !m_ignore_missing_includes )	throw script::Exception(script::EResult_IncludeFileNotFound, "Included file not found");
				else								return;
			}

			pr::filesys::Standardise(filename);

			// Read the include file contents
			std::string extra_source;
			if( !FileToBuffer(filename.c_str(), extra_source) ) throw script::Exception(script::EResult_LoadIncludeFailed, "Failed to read the contents of an included file");

			// Insert it into 'm_source'
			m_source.insert(insert_offset, extra_source.c_str());

			// Save the included file in 'm_included_files'
			pr::script::TPaths::iterator iter = std::find(m_included_files.begin(), m_included_files.end(), filename);
			if( iter == m_included_files.end() ) m_included_files.push_back(filename);
		}

		// Return true if 'ch' is a delimiter
		template <typename T>
		bool ScriptLoader<T>::IsDelimiter(char ch) const
		{
			return isspace(ch) || m_delimiters.find(ch) != std::string::npos;
		}

		// Return true if 'ch' is a control character
		template <typename T>
		bool ScriptLoader<T>::IsControlChar(char ch) const
		{
			return ch == m_keyword_identifier || ch == m_section_start || ch == m_section_end;
		}

		//*****
		// Skip over white space
		template <typename T>
		void ScriptLoader<T>::SkipWhiteSpace()
		{
			while( m_pos != m_last && IsDelimiter(*m_pos) ) { ++m_pos; }
		}

	}//namespace impl
}//namespace pr

#endif//PRSCRIPTLOADER_H