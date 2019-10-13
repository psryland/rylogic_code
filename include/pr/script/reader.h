//**********************************
// Script
//  Copyright (c) Rylogic Ltd 2015
//**********************************

#pragma once

#include "pr/maths/maths.h"
#include "pr/script/forward.h"
#include "pr/script/script_core.h"
#include "pr/script/preprocessor.h"
#include "pr/script/fail_policy.h"

namespace pr::script
{
	struct Reader
	{
		// Notes:
		//  - The extract functions come in four forms:
		//      Thing Thing();
		//      Thing ThingS();
		//      bool Thing(Thing&);
		//      bool ThingS(Thing&);
		//    All functions read one instance of type 'Thing' from the script.
		//    The postfix 'S' means 'within a section'. e.g. { "value" }
		//    If an error occurs in the first two forms, ReportError is called. If ReportError doesn't throw, then a default instance is returned.
		//    If an error occurs in the second two forms,  ReportError is called. If ReportError doesn't throw, then false is returned.

	private:
		Preprocessor m_pp;
		string m_delim;
		string m_last_keyword;
		bool m_case_sensitive;

	public:

		Reader(bool case_sensitive = false, IIncludeHandler* inc = nullptr, IMacroHandler* mac = nullptr, EmbeddedCodeFactory emb = nullptr) noexcept
			:m_pp(inc, mac, emb)
			,m_delim(L" \t\r\n\v,;")
			,m_last_keyword()
			,m_case_sensitive(case_sensitive)
		{}
		Reader(Src& src, bool case_sensitive = false, IIncludeHandler* inc = nullptr, IMacroHandler* mac = nullptr, EmbeddedCodeFactory emb = nullptr)
			:m_pp(src, inc, mac, emb)
			,m_delim(L" \t\r\n\v,;")
			,m_last_keyword()
			,m_case_sensitive(case_sensitive)
		{}
		Reader(Src* src, bool delete_on_pop, bool case_sensitive = false, IIncludeHandler* inc = nullptr, IMacroHandler* mac = nullptr, EmbeddedCodeFactory emb = nullptr)
			:m_pp(src, delete_on_pop, inc, mac, emb)
			,m_delim(L" \t\r\n\v,;")
			,m_last_keyword()
			,m_case_sensitive(case_sensitive)
		{}
		Reader(char const* src, bool case_sensitive = false, IIncludeHandler* inc = nullptr, IMacroHandler* mac = nullptr, EmbeddedCodeFactory emb = nullptr)
			:m_pp(src, inc, mac, emb)
			,m_delim(L" \t\r\n\v,;")
			,m_last_keyword()
			,m_case_sensitive(case_sensitive)
		{}
		Reader(wchar_t const* src, bool case_sensitive = false, IIncludeHandler* inc = nullptr, IMacroHandler* mac = nullptr, EmbeddedCodeFactory emb = nullptr)
			:m_pp(src, inc, mac, emb)
			,m_delim(L" \t\r\n\v,;")
			,m_last_keyword()
			,m_case_sensitive(case_sensitive)
		{}
		virtual ~Reader()
		{}

		// Access the underlying source
		Src const& Source() const noexcept
		{
			return m_pp;
		}
		Src& Source() noexcept
		{
			return m_pp;
		}

		// Return the current source location
		Location Loc() const
		{
			return Source().Loc();
		}

		// Access the include handler
		IIncludeHandler& Includes() const noexcept
		{
			return *m_pp.Includes;
		}

		// Access the macro handler
		IMacroHandler& Macros() const noexcept
		{
			return *m_pp.Macros;
		}

		// Get/Set delimiter characters
		wchar_t const* Delimiters() const
		{
			return m_delim.c_str();
		}
		void Delimiters(wchar_t const* delim) noexcept
		{
			m_delim = delim;
		}

		// Get/Set case sensitive keywords on/off
		bool CaseSensitive() const noexcept
		{
			return m_case_sensitive;
		}
		void CaseSensitive(bool cs) noexcept
		{
			m_case_sensitive = cs;
		}

		// Return the hash of a keyword using the current reader settings
		static int StaticHashKeyword(wchar_t const* keyword, bool case_sensitive)
		{
			// This function can't have the same name as 'HashKeyword' because it causes a compiler error
			auto const kw = case_sensitive ? pr::hash::Hash(keyword) : pr::hash::HashI(keyword);
			return kw;
		}
		int HashKeyword(wchar_t const* keyword) const
		{
			return StaticHashKeyword(keyword, m_case_sensitive);
		}

		// Return true if the end of the source has been reached
		bool IsSourceEnd()
		{
			auto& src = m_pp;
			EatDelimiters(src, m_delim.c_str());
			return *src == 0;
		}

		// Return true if the next token is a keyword
		bool IsKeyword()
		{
			auto& src = m_pp;
			EatDelimiters(src, m_delim.c_str());
			return *src == '*';
		}

		// Returns true if the next non-whitespace character is the start/end of a section
		bool IsSectionStart()
		{
			auto& src = m_pp;
			EatDelimiters(src, m_delim.c_str());
			return *src == L'{';
		}
		bool IsSectionEnd()
		{
			auto& src = m_pp;
			EatDelimiters(src, m_delim.c_str());
			return *src == L'}';
		}

		// Return true if the next token is not a keyword, the section end, or the end of the source
		bool IsValue()
		{
			return !IsKeyword() && !IsSectionEnd() && !IsSourceEnd();
		}

		// Move to the start/end of a section and then one past it
		bool SectionStart()
		{
			auto& src = m_pp;
			if (IsSectionStart()) { ++src; return true; }
			return ReportError(EResult::TokenNotFound, src.Loc(), "expected '{'");
		}
		bool SectionEnd()
		{
			auto& src = m_pp;
			if (IsSectionEnd()) { ++src; return true; }
			return ReportError(EResult::TokenNotFound, src.Loc(), "expected '}'");
		}

		// Move to the start of the next line
		bool NewLine()
		{
			auto& src = m_pp;
			EatLine(src, 0, 0);
			if (pr::str::IsNewLine(*src)) ++src; else return false;
			return true;
		}

		// Advance the source to the next '{' within the current scope
		// On return the current position should be a section start character
		// or the end of the current section or end of the input stream if not found
		bool FindSectionStart()
		{
			auto& src = m_pp;
			for (;*src && *src != L'{' && *src != L'}';)
			{
				if (*src == L'\"') { EatLiteralString(src); continue; }
				else ++src;
			}
			return *src == L'{';
		}

		// Advance the source to the end of the current section
		// On return the current position should be the section end character
		// or the end of the input stream (if called from file scope).
		bool FindSectionEnd()
		{
			auto& src = m_pp;
			for (int nest = IsSectionStart() ? 0 : 1; *src;)
			{
				if (*src == L'\"') { EatLiteralString(src); continue; }
				nest += (*src == L'{') ? 1 : 0;
				nest -= (*src == L'}') ? 1 : 0;
				if (nest == 0) break;
				++src;
			}
			return *src == L'}';
		}

		// Scans forward until a keyword identifier is found within the current scope.
		// Non-keyword tokens are skipped. If a section is found it is skipped.
		// If a keyword is found, the source is position at the next character after the keyword
		// Returns true if a keyword is found, false otherwise.
		template <typename StrType> bool NextKeywordS(StrType& kw)
		{
			auto& src = m_pp;
			for (;*src && *src != L'}' && *src != L'*';)
			{
				if (*src == L'\"') { EatLiteralString(src); continue; }
				if (*src == L'{')  { src += FindSectionEnd(); continue; }
				++src;
			}
			if (*src == L'*') ++src; else return false;
			pr::str::Resize(kw, 0);
			if (!pr::str::ExtractIdentifier(kw, src, m_delim.c_str())) return false;
			if (!m_case_sensitive) pr::str::LowerCase(kw);
			m_last_keyword = pr::Widen(kw);
			return true;
		}

		// As above except the hash of the keyword is returned instead (converted to an enum value)
		template <typename Enum> bool NextKeywordH(Enum& enum_kw)
		{
			string kw;
			if (!NextKeywordS(kw)) return false;
			m_last_keyword = kw;
			enum_kw = Enum(HashKeyword(kw.c_str()));
			return true;
		}

		// As above except an error is reported if the next token is not a keyword
		int NextKeywordH()
		{
			int kw = 0;
			if (!NextKeywordH(kw)) ReportError(EResult::TokenNotFound, m_pp.Loc(), "keyword expected");
			return kw;
		}
		template <typename Enum> Enum NextKeywordH()
		{
			return Enum(NextKeywordH());
		}

		// Return the last keyword reader from the stream
		string LastKeyword() const
		{
			return m_last_keyword;
		}

		// Scans forward until a keyword matching 'named_kw' is found within the current scope.
		// Returns false if the named keyword is not found, true if it is
		bool FindKeyword(wchar_t const* named_kw)
		{
			int kw_hashed = 0;
			auto const named_kw_hashed = HashKeyword(named_kw);
			for (; NextKeywordH(kw_hashed) && kw_hashed != named_kw_hashed; ) {}
			return named_kw_hashed == kw_hashed;
		}

		// Scans forward until a keyword matching 'named_kw' is found within the current scope.
		// Calls ReportError if not found.
		Reader& Keyword(wchar_t const* named_kw)
		{
			FindKeyword(named_kw) || ReportError(EResult::KeywordNotFound, FmtS("keyword '%S' expected", named_kw));
			return *this;
		}

		// Extract a token from the source.
		// A token is a contiguous block of non-separator characters
		template <typename StrType> StrType Token()
		{
			StrType token;
			return Token(token) ? token : StrType();
		}
		template <typename StrType> StrType TokenS()
		{
			StrType token;
			return TokenS(token) ? token : StrType{};
		}
		template <typename StrType> bool Token(StrType& token)
		{
			auto& src = m_pp;
			str::Resize(token, 0);
			return str::ExtractToken(token, src, m_delim.c_str()) || ReportError(EResult::TokenNotFound, "token expected");
		}
		template <typename StrType> bool TokenS(StrType& token)
		{
			return SectionStart() && Token(token) && SectionEnd();
		}

		// Extract a token using additional delimiters
		template <typename StrType> StrType Token(wchar_t const* delim)
		{
			StrType token;
			return Token(token, delim) ? x : StrType();
		}
		template <typename StrType> StrType TokenS(wchar_t const* delim)
		{
			StrType token;
			return TokenS(token, delim) ? token : StrType{};
		}
		template <typename StrType> bool Token(StrType& token, wchar_t const* delim)
		{
			auto& src = m_pp;
			str::Resize(token, 0);
			return str::ExtractToken(token, src, string(m_delim).append(delim).c_str()) || ReportError(EResult::TokenNotFound, "token expected");
		}
		template <typename StrType> bool TokenS(StrType& token, wchar_t const* delim)
		{
			return SectionStart() && Token(token, delim) && SectionEnd();
		}

		// Read an identifier from the source. An identifier is one of (A-Z,a-z,'_') followed by (A-Z,a-z,'_',0-9) in a contiguous block
		template <typename StrType> StrType Identifier()
		{
			StrType word;
			return Identifier(word) ? word : StrType{};
		}
		template <typename StrType> StrType IdentifierS()
		{
			StrType word;
			return IdentifierS(word) ? word : StrType{};
		}
		template <typename StrType> bool Identifier(StrType& word)
		{
			auto& src = m_pp;
			str::Resize(word, 0);
			return str::ExtractIdentifier(word, src, m_delim.c_str()) || ReportError(EResult::TokenNotFound, "{identifier} expected");
		}
		template <typename StrType> bool IdentifierS(StrType& word)
		{
			return SectionStart() && Identifier(word) && SectionEnd();
		}

		// Extract identifiers from the source separated by 'sep'
		template <typename StrType> bool Identifiers(char, StrType& word)
		{
			auto& src = m_pp;
			str::Resize(word, 0);
			return str::ExtractIdentifier(word, src, m_delim.c_str()) || ReportError(EResult::TokenNotFound, "identifier expected");
		}
		template <typename StrType, typename... StrTypes> bool Identifiers(char sep, StrType& word, StrTypes&&... words)
		{
			auto& src = m_pp;
			str::Resize(word, 0);
			if (!str::ExtractIdentifier(word, src, m_delim.c_str())) return ReportError(EResult::TokenNotFound, "identifier expected");
			if (*src == sep) ++src; else return ReportError(EResult::TokenNotFound, "identifier separator expected");
			return Identifiers(sep, std::forward<StrTypes>(words)...);
		}
		template <typename StrType, typename... StrTypes> bool IdentifiersS(char sep, StrType& word, StrTypes&&... words)
		{
			return SectionStart() && Identifiers(sep,word,std::forward<StrTypes>(words)...) && SectionEnd();
		}

		// Extract a string from the source. A string is a sequence of characters between quotes.
		template <typename StrType> StrType String()
		{
			StrType string;
			return String(string) ? string : StrType{};
		}
		template <typename StrType> StrType StringS()
		{
			StrType string;
			return StringS(string) ? string : StrType{};
		}
		template <typename StrType> bool String(StrType& string)
		{
			auto& src = m_pp;
			str::Resize(string, 0);
			if (str::ExtractString<StrType>(string, src, m_delim.c_str()))
			{
				str::ProcessIndentedNewlines(string);
				return true;
			}
			return ReportError(EResult::TokenNotFound, "string expected");
		}
		template <typename StrType> bool StringS(StrType& string)
		{
			return SectionStart() && String(string) && SectionEnd();
		}

		// Extract a C-style string from the source.
		template <typename StrType> StrType CString()
		{
			StrType cstring;
			return CString(cstring) ? cstring : StrType{};
		}
		template <typename StrType> StrType CStringS()
		{
			StrType cstring;
			return CStringS(cstring) ? cstring : StrType{};
		}
		template <typename StrType> bool CString(StrType& cstring)
		{
			auto& src = m_pp;
			str::Resize(cstring, 0);
			return str::ExtractString<StrType>(cstring, src, L'\\', nullptr, m_delim.c_str()) || ReportError(EResult::TokenNotFound, "'cstring' expected");
		}
		template <typename StrType> bool CStringS(StrType& cstring)
		{
			return SectionStart() && CString(cstring) && SectionEnd();
		}

		// Extract a bool from the source.
		template <typename TBool> TBool Bool()
		{
			TBool bool_;
			return Bool(bool_) ? bool_ : TBool{};
		}
		template <typename TBool> TBool BoolS()
		{
			TBool bool_;
			return BoolS(bool_) ? bool_ : TBool{};
		}
		template <typename TBool> bool Bool(TBool& bool_)
		{
			auto& src = m_pp;
			return str::ExtractBool(bool_, src, m_delim.c_str()) || ReportError(EResult::TokenNotFound, "bool expected");
		}
		template <typename TBool> bool BoolS(TBool& bool_)
		{
			return SectionStart() && Bool(bool_) && SectionEnd();
		}
		template <typename TBool> bool Bool(TBool* bools, size_t num_bools)
		{
			for (; num_bools && Bool(*bools); --num_bools, ++bools) {}
			return num_bools == 0;
		}
		template <typename TBool> bool BoolS(TBool* bools, size_t num_bools)
		{
			return SectionStart() && Bool(bools, num_bools) && SectionEnd();
		}

		// Extract an integral type from the source.
		template <typename TInt> TInt Int(int radix)
		{
			TInt int_;
			return Int(int_, radix) ? int_ : TInt{};
		}
		template <typename TInt> TInt IntS(int radix)
		{
			TInt int_;
			return IntS(int_, radix) ? int_ : TInt{};
		}
		template <typename TInt> bool Int(TInt& int_, int radix)
		{
			auto& src = m_pp;
			return str::ExtractInt(int_, radix, src, m_delim.c_str()) || ReportError(EResult::TokenNotFound, "integral value expected");
		}
		template <typename TInt> bool IntS(TInt& int_, int radix)
		{
			return SectionStart() && Int(int_, radix) && SectionEnd();
		}
		template <typename TInt> bool Int(TInt* ints, size_t num_ints, int radix)
		{
			for (; num_ints && Int(*ints, radix); --num_ints, ++ints) {}
			return num_ints == 0;
		}
		template <typename TInt> bool IntS(TInt* ints, size_t num_ints, int radix)
		{
			return SectionStart() && Int(ints, num_ints, radix) && SectionEnd();
		}

		// Extract a real from the source.
		template <typename TReal> TReal Real()
		{
			TReal real_;
			return Real(real_) ? real_ : TReal{};
		}
		template <typename TReal> TReal RealS()
		{
			TReal real_;
			return RealS(real_) ? real_ : TReal{};
		}
		template <typename TReal> bool Real(TReal& real_)
		{
			auto& src = m_pp;
			return str::ExtractReal(real_, src, m_delim.c_str()) || ReportError(EResult::TokenNotFound, "real expected");
		}
		template <typename TReal> bool RealS(TReal& real_)
		{
			return SectionStart() && Real(real_) && SectionEnd();
		}
		template <typename TReal> bool Real(TReal* reals, size_t num_reals)
		{
			for (; num_reals && Real(*reals); --num_reals, ++reals) {}
			return num_reals == 0;
		}
		template <typename TReal> bool RealS(TReal* reals, size_t num_reals)
		{
			return SectionStart() && Real(reals, num_reals) && SectionEnd();
		}

		// Extract an enum value from the source.
		template <typename TEnum> TEnum EnumValue()
		{
			TEnum enum_;
			return EnumValue(enum_) ? enum_ : TEnum{};
		}
		template <typename TEnum> TEnum EnumValueS()
		{
			TEnum enum_;
			return EnumValueS(enum_) ? enum_ : TEnum{};
		}
		template <typename TEnum> bool EnumValue(TEnum& enum_)
		{
			auto& src = m_pp;
			return str::ExtractEnumValue(enum_, src, m_delim.c_str()) || ReportError(EResult::TokenNotFound, "enum integral value expected");
		}
		template <typename TEnum> bool EnumValueS(TEnum& enum_)
		{
			return SectionStart() && EnumValue(enum_) && SectionEnd();
		}

		// Extract an enum identifier from the source.
		template <typename TEnum> TEnum Enum()
		{
			TEnum enum_;
			return Enum(enum_) ? enum_ : TEnum{};
		}
		template <typename TEnum> TEnum EnumS()
		{
			TEnum enum_;
			return EnumS(enum_) ? enum_ : TEnum{};
		}
		template <typename TEnum> bool Enum(TEnum& enum_)
		{
			auto& src = m_pp;
			return str::ExtractEnum(enum_, src, m_delim.c_str()) || ReportError(EResult::TokenNotFound, "enum member string name expected");
		}
		template <typename TEnum> bool EnumS(TEnum& enum_)
		{
			return SectionStart() && Enum(enum_) && SectionEnd();
		}

		// Extract a 2D real vector from the source
		v2 Vector2()
		{
			v2 vector;
			return Vector2(vector) ? vector : v2{};
		}
		v2 Vector2S()
		{
			v2 vector;
			return Vector2S(vector) ? vector : v2{};
		}
		bool Vector2(v2& vector)
		{
			return Real(vector.x) && Real(vector.y);
		}
		bool Vector2S(v2& vector)
		{
			return SectionStart() && Vector2(vector) && SectionEnd();
		}

		// Extract a 2D integer vector from the source
		iv2 Vector2i(int radix = 10)
		{
			iv2 vector;
			return Vector2i(vector, radix) ? vector : iv2{};
		}
		iv2 Vector2iS(int radix = 10)
		{
			iv2 vector;
			return Vector2iS(vector, radix) ? vector : iv2{};
		}
		bool Vector2i(iv2& vector, int radix = 10)
		{
			return Int(vector.x, radix) && Int(vector.y, radix);
		}
		bool Vector2iS(iv2& vector, int radix = 10)
		{
			return SectionStart() && Vector2i(vector, radix) && SectionEnd();
		}

		// Extract a 3D real vector from the source
		v4 Vector3(float w)
		{
			v4 vector;
			return Vector3(vector, w) ? vector : v4{};
		}
		v4 Vector3S(float w)
		{
			v4 vector;
			return Vector3S(vector, w) ? vector : v4{};
		}
		bool Vector3(v4& vector, float w)
		{
			vector.w = w;
			return Real(vector.x) && Real(vector.y) && Real(vector.z);
		}
		bool Vector3S(v4& vector, float w)
		{
			return SectionStart() && Vector3(vector, w) && SectionEnd();
		}

		// Extract a 3D integer vector from the source
		iv4 Vector3i(int w, int radix = 10)
		{
			iv4 vector;
			return Vector3i(vector, w, radix) ? vector : iv4{};
		}
		iv4 Vector3iS(int w, int radix = 10)
		{
			iv4 vector;
			return Vector3iS(vector, w, radix) ? vector : iv4{};
		}
		bool Vector3i(iv4& vector, int w, int radix = 10)
		{
			vector.w = w;
			return Int(vector.x, radix) && Int(vector.y, radix) && Int(vector.z, radix);
		}
		bool Vector3iS(iv4& vector, int w, int radix = 10)
		{
			return SectionStart() && Vector3i(vector, w, radix) && SectionEnd();
		}

		// Extract a 4D real vector from the source
		v4 Vector4()
		{
			v4 vector;
			return Vector4(vector) ? vector : v4{};
		}
		v4 Vector4S()
		{
			v4 vector;
			return Vector4S(vector) ? vector : v4{};
		}
		bool Vector4(v4& vector)
		{
			return Real(vector.x) && Real(vector.y) && Real(vector.z) && Real(vector.w);
		}
		bool Vector4S(v4& vector)
		{
			return SectionStart() && Vector4(vector) && SectionEnd();
		}

		// Extract a 4D integer vector from the source
		iv4 Vector4i(int radix = 10)
		{
			iv4 vector;
			return Vector4i(vector, radix) ? vector : iv4{};
		}
		iv4 Vector4iS(int radix = 10)
		{
			iv4 vector;
			return Vector4iS(vector, radix) ? vector : iv4{};
		}
		bool Vector4i(iv4& vector, int radix = 10)
		{
			return Int(vector.x, radix) && Int(vector.y, radix) && Int(vector.z, radix) && Int(vector.w, radix);
		}
		bool Vector4iS(iv4& vector, int radix = 10)
		{
			return SectionStart() && Vector4i(vector, radix) && SectionEnd();
		}

		// Extract a quaternion from the source
		quat Quaternion()
		{
			quat quaternion;
			return Quaternion(quaternion) ? quaternion : quat{};
		}
		quat QuaternionS()
		{
			quat quaternion;
			return QuaternionS(quaternion) ? quaternion : quat{};
		}
		bool Quaternion(quat& quaternion)
		{
			return Real(quaternion.x) && Real(quaternion.y) && Real(quaternion.z) && Real(quaternion.w);
		}
		bool QuaternionS(quat& quaternion)
		{
			return SectionStart() && Quaternion(quaternion) && SectionEnd();
		}

		// Extract a 3x3 matrix from the source
		m3x4 Matrix3x3()
		{
			m3x4 transform;
			return Matrix3x3(transform) ? transform : m3x4{};
		}
		m3x4 Matrix3x3S()
		{
			m3x4 transform;
			return Matrix3x3S(transform) ? transform : m3x4{};
		}
		bool Matrix3x3(m3x4& transform)
		{
			return Vector3(transform.x, 0) && Vector3(transform.y, 0) && Vector3(transform.z, 0);
		}
		bool Matrix3x3S(m3x4& transform)
		{
			return SectionStart() && Matrix3x3(transform) && SectionEnd();
		}

		// Extract a 4x4 matrix from the source
		m4x4 Matrix4x4()
		{
			m4x4 transform;
			return Matrix4x4(transform) ? transform : m4x4{};
		}
		m4x4 Matrix4x4S()
		{
			m4x4 transform;
			return Matrix4x4S(transform) ? transform : m4x4{};
		}
		bool Matrix4x4(m4x4& transform)
		{
			return Vector4(transform.x) && Vector4(transform.y) && Vector4(transform.z) && Vector4(transform.w);
		}
		bool Matrix4x4S(m4x4& transform)
		{
			return SectionStart() && Matrix4x4(transform) && SectionEnd();
		}

		// Extract a byte array
		vector<uint8_t> Data(size_t length, int radix = 16)
		{
			vector<uint8_t> data(length);
			return Data(data.data(), length, radix) ? std::move(data) : vector<uint8_t>{};
		}
		vector<uint8_t> DataS(size_t length, int radix = 16)
		{
			vector<uint8_t> data(length);
			return DataS(data.data(), length, radix) ? std::move(data) : vector<uint8_t>{};
		}
		bool Data(void* data, size_t length, int radix = 16)
		{
			return Int(static_cast<uint8_t*>(data), length, radix);
		}
		bool DataS(void* data, size_t length, int radix = 16)
		{
			return SectionStart() && Data(data, length, radix) && SectionEnd();
		}

		// Extract a transform description. 'rot' should be a valid initial transform
		m3x4 Rotation()
		{
			m3x4 rot = m3x4Identity;
			return Rotation(rot) ? rot : m3x4Identity;
		}
		m3x4 RotationS()
		{
			m3x4 rot = m3x4Identity;
			return RotationS(rot) ? rot : m3x4Identity;
		}
		bool Rotation(m3x4& rot)
		{
			assert(IsFinite(rot) && "A valid 'rot' must be passed to this function as it pre-multiplies the transform with the one read from the script");
			auto o2w = m4x4{ rot, v4Origin };
			return Transform(o2w) ? (rot = o2w.rot, true) : false;
		}
		bool RotationS(m3x4& o2w)
		{
			return SectionStart() && Rotation(o2w) && SectionEnd();
		}

		// Extract a transform description accumulatively. 'o2w' should be a valid initial transform.
		m4x4 Transform()
		{
			m4x4 o2w = m4x4Identity;
			return Transform(o2w) ? o2w : m4x4{};
		}
		m4x4 TransformS()
		{
			m4x4 o2w = m4x4Identity;
			return TransformS(o2w) ? o2w : m4x4{};
		}
		bool Transform(m4x4& o2w)
		{
			assert(IsFinite(o2w) && "A valid 'o2w' must be passed to this function as it pre-multiplies the transform with the one read from the script");
			auto p2w = m4x4Identity;

			// Tranform keywords
			const int M4x4           = HashKeyword(L"M4x4"          );
			const int M3x3           = HashKeyword(L"M3x3"          );
			const int Pos            = HashKeyword(L"Pos"           );
			const int Align          = HashKeyword(L"Align"         );
			const int Quat           = HashKeyword(L"Quat"          );
			const int QuatPos        = HashKeyword(L"QuatPos"       );
			const int Rand4x4        = HashKeyword(L"Rand4x4"       );
			const int RandPos        = HashKeyword(L"RandPos"       );
			const int RandOri        = HashKeyword(L"RandOri"       );
			const int Euler          = HashKeyword(L"Euler"         );
			const int Scale          = HashKeyword(L"Scale"         );
			const int Transpose      = HashKeyword(L"Transpose"     );
			const int Inverse        = HashKeyword(L"Inverse"       );
			const int Normalise      = HashKeyword(L"Normalise"     );
			const int Orthonormalise = HashKeyword(L"Orthonormalise");
				
			// Parse the transform
			for (int kw; NextKeywordH(kw);)
			{
				if (kw == M4x4)
				{
					auto m = m4x4Identity;
					Matrix4x4S(m);
					if (m.w.w != 1)
					{
						ReportError(script::EResult::UnknownValue, "M4x4 must be an affine transform with: w.w == 1");
						break;
					}
					p2w = m * p2w;
					continue;
				}
				if (kw == M3x3)
				{
					auto m = m4x4Identity;
					Matrix3x3S(m.rot);
					p2w = m * p2w;
					continue;
				}
				if (kw == Pos)
				{
					auto m = m4x4Identity;
					Vector3S(m.pos, 1.0f);
					p2w = m * p2w;
					continue;
				}
				if (kw == Align)
				{
					int axis_id;
					v4 direction;
					SectionStart();
					Int(axis_id, 10);
					Vector3(direction, 0.0f);
					SectionEnd();

					v4 axis = AxisId(axis_id);
					if (IsZero3(axis))
					{
						ReportError(script::EResult::UnknownValue, "axis_id must one of �1, �2, �3");
						break;
					}

					p2w = m4x4::Transform(axis, direction, v4Origin) * p2w;
					continue;
				}
				if (kw == Quat)
				{
					quat q;
					Vector4S(q.xyzw);
					p2w = m4x4::Transform(q, v4Origin) * p2w;
					continue;
				}
				if (kw == QuatPos)
				{
					v4 p;
					quat q;
					SectionStart();
					Vector4(q.xyzw);
					Vector3(p, 1.0f);
					SectionEnd();
					p2w = m4x4::Transform(q, p.w1()) * p2w;
					continue;
				}
				if (kw == Rand4x4)
				{
					float radius;
					v4 centre;
					SectionStart();
					Vector3(centre, 1.0f);
					Real(radius);
					SectionEnd();
					p2w = Random4x4(g_rng(), centre, radius) * p2w;
					continue;
				}
				if (kw == RandPos)
				{
					float radius;
					v4 centre;
					SectionStart();
					Vector3(centre, 1.0f);
					Real(radius);
					SectionEnd();
					p2w = m4x4::Translation(Random3(g_rng(), centre, radius, 1.0f)) * p2w;
					continue;
				}
				if (kw == RandOri)
				{
					m4x4 m = m4x4Identity;
					m.rot = Random3x4(g_rng());
					p2w = m * p2w;
					continue;
				}
				if (kw == Euler)
				{
					v4 angles;
					Vector3S(angles, 0.0f);
					p2w = m4x4::Transform(DegreesToRadians(angles.x), DegreesToRadians(angles.y), DegreesToRadians(angles.z), v4Origin) * p2w;
					continue;
				}
				if (kw == Scale)
				{
					v4 scale;
					SectionStart();
					Real(scale.x);
					if (IsSectionEnd())
					{
						scale.z = scale.y = scale.x;
					}
					else
					{
						Real(scale.y);
						Real(scale.z);
					}
					SectionEnd();
					p2w = m4x4::Scale(scale.x, scale.y, scale.z, v4Origin) * p2w;
					continue;
				}
				if (kw == Transpose)
				{
					p2w = Transpose4x4(p2w);
					continue;
				}
				if (kw == Inverse)
				{
					p2w = IsOrthonormal(p2w) ? InvertFast(p2w) : Invert(p2w);
					continue;
				}
				if (kw == Normalise)
				{
					p2w.x = Normalise3(p2w.x);
					p2w.y = Normalise3(p2w.y);
					p2w.z = Normalise3(p2w.z);
					continue;
				}
				if (kw == Orthonormalise)
				{
					p2w = Orthonorm(p2w);
					continue;
				}
				ReportError(script::EResult::UnknownToken);
				break;
			}

			// Pre-multiply the object to world transform
			o2w = p2w * o2w;
			return true;
		}
		bool TransformS(m4x4& o2w)
		{
			return SectionStart() && Transform(o2w) && SectionEnd();
		}

		// Extract a complete section as a preprocessed string. Note: To embed arbitrary text in a script use #lit/#end and then Section()
		template <typename String> String Section(bool include_braces)
		{
			String s;
			return Section(s, include_braces) ? s : String{};
		}
		template <typename String> bool Section(String& str, bool include_braces)
		{
			// Do not str.resize(0) here, that's the callers decision
			auto& src = m_pp;
			StringLit lit;
			if (IsSectionStart()) ++src; else return ReportError(EResult::TokenNotFound, "expected '{'");
			if (include_braces) pr::str::Append(str, L'{');
			for (int nest = 1; *src; ++src)
			{
				// If we're in a string/character literal, then ignore any '{''}' characters
				if (lit.inc(*src)) { pr::str::Append(str, *src); continue; }
				nest += int(*src == '{');
				nest -= int(*src == '}');
				if (nest == 0) break;
				pr::str::Append(str, *src);
			}
			if (include_braces) pr::str::Append(str, '}');
			if (IsSectionEnd()) ++src; else return ReportError(EResult::TokenNotFound, "expected '}'");
			return true;
		}

		// Allow extension methods. e.g: template <> bool pr::script::Reader::Extract<MyType>(MyType& my_type) { return Int(my_type.int, 10); // etc }
		template <typename Type> Type Extract()
		{
			Type type;
			return Extract(type) ? std::move(type) : Type{};
		}
		template <typename Type> Type ExtractS()
		{
			Type type;
			return ExtractS(type) ? std::move(type) : Type{};
		}
		template <typename Type> bool Extract(Type& type)
		{
			static_assert(false, "Extract method not implemented for this type");
		}
		template <typename Type> bool ExtractS(Type& type)
		{
			return SectionStart() && ExtractS(type) && SectionEnd();
		}

		// Allow subclasses report errors
		bool ReportError(EResult result)
		{
			return ReportError(result, ToStringA(result));
		}
		bool ReportError(EResult result, char const* msg)
		{
			auto const& src = m_pp;
			return ReportError(result, src.Loc(), msg);
		}
		virtual bool ReportError(EResult result, Location const& loc, char const* msg)
		{
			throw Exception(result, loc, msg);
		}
	};
}


#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::script
{
	PRUnitTest(ReaderTests)
	{
		char const* src =
			"#define NUM 23\n"
			"*Identifier ident\n"
			"*String \"simple string\"\n"
			"*CString \"C:\\\\Path\\\\Filename.txt\"\n"
			"*Bool true\n"
			"*Intg -NUM\n"
			"*Intg16 ABCDEF00\n"
			"*Real -2.3e+3\n"
			"*BoolArray 1 0 true false\n"
			"*IntArray -3 2 +1 -0\n"
			"*RealArray 2.3 -1.0e-1 2 -0.2\n"
			"*Vector3 1.0 2.0 3.0\n"
			"*Vector4 4.0 3.0 2.0 1.0\n"
			"*Quaternion 0.0 -1.0 -2.0 -3.0\n"
			"*M3x3 1.0 0.0 0.0  0.0 1.0 0.0  0.0 0.0 1.0\n"
			"*M4x4 1.0 0.0 0.0 0.0  0.0 1.0 0.0 0.0  0.0 0.0 1.0 0.0  0.0 0.0 0.0 1.0\n"
			"*Data 41 42 43 44 45 46 47 48 49 4A 4B 4C 4D 4E 4F 00\n"
			"*Junk\n"
			"*Section {*SubSection { *Data \n NUM \"With a }\\\"string\\\"{ in it\" }}    \n"
			"*Section {*SubSection { *Data \n NUM \"With a }\\\"string\\\"{ in it\" }}    \n"
			"*Token 123token\n"
			"*LastThing";

		char kw[50];
		int hashed_kw = 0;
		std::string str;
		bool bval = false, barray[4];
		int ival = 0, iarray[4];
		unsigned int uival = 0;
		float fval = 0.0f, farray[4];
		pr::v4 vec = pr::v4Zero;
		pr::quat q = pr::QuatIdentity;
		pr::m3x4 mat3;
		pr::m4x4 mat4;

		{// basic extract methods
			Reader reader(src, true);
			PR_CHECK(reader.CaseSensitive()         ,true);
			PR_CHECK(reader.NextKeywordS(kw)        ,true); PR_CHECK(std::string(kw) , "Identifier"                  );
			PR_CHECK(reader.Identifier(str)         ,true); PR_CHECK(str             , "ident"                       );
			PR_CHECK(reader.NextKeywordS(kw)        ,true); PR_CHECK(std::string(kw) , "String"                      );
			PR_CHECK(reader.String(str)             ,true); PR_CHECK(str             , "simple string"               );
			PR_CHECK(reader.NextKeywordH(hashed_kw) ,true); PR_CHECK(hashed_kw       , reader.HashKeyword(L"CString"));
			PR_CHECK(reader.CString(str)            ,true); PR_CHECK(str             , "C:\\Path\\Filename.txt"      );
			PR_CHECK(reader.NextKeywordS(kw)        ,true); PR_CHECK(std::string(kw) , "Bool"                        );
			PR_CHECK(reader.Bool(bval)              ,true); PR_CHECK(bval            , true                          );
			PR_CHECK(reader.NextKeywordS(kw)        ,true); PR_CHECK(std::string(kw) , "Intg"                        );
			PR_CHECK(reader.Int(ival, 10)           ,true); PR_CHECK(ival            , -23                           );
			PR_CHECK(reader.NextKeywordS(kw)        ,true); PR_CHECK(std::string(kw) , "Intg16"                      );
			PR_CHECK(reader.Int(uival, 16)          ,true); PR_CHECK(uival           , 0xABCDEF00                    );
			PR_CHECK(reader.NextKeywordS(kw)        ,true); PR_CHECK(std::string(kw) , "Real"                        );
			PR_CHECK(reader.Real(fval)              ,true); PR_CHECK(fval            , -2.3e+3                       );
			PR_CHECK(reader.NextKeywordS(kw)        ,true); PR_CHECK(std::string(kw) , "BoolArray"                   );
			PR_CHECK(reader.Bool(barray, 4)         ,true);
			PR_CHECK(barray[0], true );
			PR_CHECK(barray[1], false);
			PR_CHECK(barray[2], true );
			PR_CHECK(barray[3], false);
			PR_CHECK(reader.NextKeywordS(kw)   ,true); PR_CHECK(std::string(kw) , "IntArray");
			PR_CHECK(reader.Int(iarray, 4, 10) ,true);
			PR_CHECK(iarray[0], -3);
			PR_CHECK(iarray[1], +2);
			PR_CHECK(iarray[2], +1);
			PR_CHECK(iarray[3], -0);
			PR_CHECK(reader.NextKeywordS(kw) ,true); PR_CHECK(std::string(kw) , "RealArray");
			PR_CHECK(reader.Real(farray, 4)  ,true);
			PR_CHECK(farray[0], 2.3f    );
			PR_CHECK(farray[1], -1.0e-1f);
			PR_CHECK(farray[2], +2.0f   );
			PR_CHECK(farray[3], -0.2f   );
			PR_CHECK(reader.NextKeywordS(kw)            ,true); PR_CHECK(std::string(kw) , "Vector3");
			PR_CHECK(reader.Vector3(vec,-1.0f)          ,true); PR_CHECK(pr::FEql4(vec, pr::v4(1.0f, 2.0f, 3.0f,-1.0f)), true);
			PR_CHECK(reader.NextKeywordS(kw)            ,true); PR_CHECK(std::string(kw) , "Vector4");
			PR_CHECK(reader.Vector4(vec)                ,true); PR_CHECK(pr::FEql4(vec, pr::v4(4.0f, 3.0f, 2.0f, 1.0f)), true);
			PR_CHECK(reader.NextKeywordS(kw)            ,true); PR_CHECK(std::string(kw) , "Quaternion");
			PR_CHECK(reader.Quaternion(q)               ,true); PR_CHECK(pr::FEql4(q, pr::quat(0.0f, -1.0f, -2.0f, -3.0f)), true);
			PR_CHECK(reader.NextKeywordS(kw)            ,true); PR_CHECK(std::string(kw) , "M3x3");
			PR_CHECK(reader.Matrix3x3(mat3)             ,true); PR_CHECK(pr::FEql(mat3, pr::m3x4Identity), true);
			PR_CHECK(reader.NextKeywordS(kw)            ,true); PR_CHECK(std::string(kw) , "M4x4");
			PR_CHECK(reader.Matrix4x4(mat4)             ,true); PR_CHECK(pr::FEql(mat4, pr::m4x4Identity), true);
			PR_CHECK(reader.NextKeywordS(kw)            ,true); PR_CHECK(std::string(kw) , "Data");
			PR_CHECK(reader.Data(kw, 16)                ,true); PR_CHECK(std::string(kw) , "ABCDEFGHIJKLMNO");
			PR_CHECK(reader.FindKeyword(L"Section") ,true); str.resize(0);
			PR_CHECK(reader.Section(str, false)         ,true); PR_CHECK(str, "*SubSection { *Data \n 23 \"With a }\\\"string\\\"{ in it\" }");
			PR_CHECK(reader.FindKeyword(L"Section") ,true); str.resize(0);
			PR_CHECK(reader.NextKeywordS(kw)            ,true); PR_CHECK(std::string(kw) , "Token");
			PR_CHECK(reader.Token(str)                  ,true); PR_CHECK(str, "123token");
			PR_CHECK(reader.NextKeywordS(kw)            ,true); PR_CHECK(std::string(kw) , "LastThing");
			PR_CHECK(!reader.IsKeyword()                ,true);
			PR_CHECK(!reader.IsSectionStart()           ,true);
			PR_CHECK(!reader.IsSectionEnd()             ,true);
			PR_CHECK(reader.IsSourceEnd()               ,true);
		}
		{
			char const* s =
				"A.B\n"
				"a.b.c\n"
				"A.B.C.D\n"
				;
			std::string s0,s1,s2,s3;

			Reader reader(s);
			reader.Identifiers('.',s0,s1);        PR_CHECK(s0 == "A" && s1 == "B", true);
			reader.Identifiers('.',s0,s1,s2);     PR_CHECK(s0 == "a" && s1 == "b" && s2 == "c", true);
			reader.Identifiers('.',s0,s1,s2,s3);  PR_CHECK(s0 == "A" && s1 == "B" && s2 == "C" && s3 == "D", true);
		}
	}
}
#endif
