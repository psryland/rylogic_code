/*
 *	A Character Encoding Set Implementation
 *	Copyright(C) 2003-2013 Jinhao(cnjinhao@hotmail.com)
 *
 *	Distributed under the Boost Software License, Version 1.0.
 *	(See accompanying file LICENSE_1_0.txt or copy at
 *	http://www.boost.org/LICENSE_1_0.txt)
 *
 *	@file: nana/charset.cpp
 *	@brief: A conversion between unicode characters and multi bytes characters
 */

#include <nana/charset.hpp>
#include <utility>
#include <nana/deploy.hpp>
#include <cwchar>
#include <clocale>

#if defined(NANA_MINGW)
	#include <windows.h>
#endif

namespace nana
{
	namespace detail
	{
		class locale_initializer
		{
		public:
			static void init()
			{
				static bool initialized = false;
				if(false == initialized)
				{
					initialized = true;
					//Only set the C library locale
					std::setlocale(LC_CTYPE, "");
				}
			}
		};
		
		bool wc2mb(std::string& mbstr, const wchar_t * s)
		{
			if(0 == s || *s == 0)
			{
				mbstr.clear();
				return true;
			}
#if defined(NANA_MINGW)
			int bytes = ::WideCharToMultiByte(CP_ACP, 0, s, -1, 0, 0, 0, 0);
			if(bytes > 1)
			{
				mbstr.resize(bytes - 1);
				::WideCharToMultiByte(CP_ACP, 0, s, -1, &(mbstr[0]), bytes - 1, 0, 0);
			}
			return true;
#else
			locale_initializer::init();
			std::mbstate_t mbstate = std::mbstate_t();
			std::size_t len = std::wcsrtombs(0, &s, 0, &mbstate);
			if(len == static_cast<std::size_t>(-1))
				return false;

			if(len)
			{
				mbstr.resize(len);
				std::wcsrtombs(&(mbstr[0]), &s, len, &mbstate);
			}
			else
				mbstr.clear();
#endif
			return true;
		}
		
		bool mb2wc(std::wstring& wcstr, const char* s)
		{
			if(0 == s || *s == 0)
			{
				wcstr.clear();
				return true;
			}
#if defined(NANA_MINGW) 
			int chars = ::MultiByteToWideChar(CP_ACP, 0, s, -1, 0, 0);
			if(chars > 1)
			{
				wcstr.resize(chars - 1);
				::MultiByteToWideChar(CP_ACP, 0, s, -1, &wcstr[0], chars - 1);
			}
#else
			locale_initializer::init();
			std::mbstate_t mbstate = std::mbstate_t();
			std::size_t len = std::mbsrtowcs(0, &s, 0, &mbstate);
			if(len == static_cast<std::size_t>(-1))
				return false;
			
			if(len)
			{
				wcstr.resize(len);
				std::mbsrtowcs(&wcstr[0], &s, len, &mbstate);
			}
			else
				wcstr.clear();
#endif
			return true;
		}

		bool mb2wc(std::string& wcstr, const char* s)
		{
			if(0 == s || *s == 0)
			{
				wcstr.clear();
				return true;
			}
#if defined(NANA_MINGW) 
			int chars = ::MultiByteToWideChar(CP_ACP, 0, s, -1, 0, 0);
			if(chars > 1)
			{
				wcstr.resize((chars - 1) * sizeof(wchar_t));
				::MultiByteToWideChar(CP_ACP, 0, s, -1, reinterpret_cast<wchar_t*>(&wcstr[0]), chars - 1);
			}
#else
			locale_initializer::init();
			std::mbstate_t mbstate = std::mbstate_t();
			std::size_t len = std::mbsrtowcs(0, &s, 0, &mbstate);
			if(len == static_cast<std::size_t>(-1))
				return false;
			
			if(len)
			{
				wcstr.resize(sizeof(wchar_t) * len);
				std::mbsrtowcs(reinterpret_cast<wchar_t*>(&wcstr[0]), &s, len, &mbstate);
			}
			else
				wcstr.clear();
#endif
			return true;
		}

		unsigned long utf8char(const unsigned char*& p, const unsigned char* end)
		{
			if(p != end)
			{
				if(*p < 0x80)
				{
					return *(p++);
				}
				unsigned ch = *p;
				unsigned long code;
				if(ch < 0xC0)
				{
					p = end;
					return 0;
				}
				else if(ch < 0xE0 && (p + 1 <= end))
				{
					code = ((ch & 0x1F) << 6) | (p[1] & 0x3F);
					p += 2;
				}
				else if(ch < 0xF0 && (p + 2 <= end))
				{
					code = ((((ch & 0xF) << 6) | (p[1] & 0x3F)) << 6) | (p[2] & 0x3F);
					p += 3;
				}
				else if(ch < 0x1F && (p + 3 <= end))
				{
					code = ((((((ch & 0x7) << 6) | (p[1] & 0x3F)) << 6) | (p[2] & 0x3F)) << 6) | (p[3] & 0x3F);
					p += 4;
				}
				else
				{
					p = end;
					return 0;
				}
				return code;
			}
			return 0;
		}

		unsigned long utf16char(const unsigned char* & bytes, const unsigned char* end, bool le_or_be)
		{
			unsigned long code;
			if(le_or_be)
			{
				if((bytes + 4 <= end) && ((bytes[1] & 0xFC) == 0xD8))
				{
					//32bit encoding
					unsigned long ch0 = bytes[0] | (bytes[1] << 8);
					unsigned long ch1 = bytes[2] | (bytes[3] << 8);

					code = (ch0 & 0x3FFF << 10) | (ch1 & 0x3FFF);
					bytes += 4;
				}
				else if(bytes + 2 <= end)
				{
					code = bytes[0] | (bytes[1] << 8);
					bytes += 2;
				}
				else
				{
					bytes = end;
					return 0;
				}
			}
			else
			{
				if((bytes + 4 <= end) && ((bytes[0] & 0xFC) == 0xD8))
				{
					//32bit encoding
					unsigned long ch0 = (bytes[0] << 8) | bytes[1];
					unsigned long ch1 = (bytes[2] << 8) | bytes[3];
					code = (ch0 & 0x3FFF << 10) | (ch1 & 0x3FFF);
					bytes += 4;
				}
				else if(bytes + 2 <= end)
				{
					code = (bytes[0] << 8) | bytes[1];
					bytes += 2;
				}
				else
				{
					bytes = end;
					return 0;
				}
			}
			return code;
		}

		unsigned long utf32char(const unsigned char* & bytes, const unsigned char* end, bool le_or_be)
		{
			if(bytes + 4 <= end)
			{
				unsigned long code;
				if(le_or_be)
					code = bytes[0] | (bytes[1] << 8) | (bytes[2] << 16) | (bytes[3] << 24);
				else
					code = bytes[3] | (bytes[2] << 8) | (bytes[1] << 16) | (bytes[0] << 24);
				bytes += 4;
				return code;
			}
			bytes = end;
			return 0;
		}

		void put_utf8char(std::string& s, unsigned long code)
		{
			if(code < 0x80)
			{
				s += static_cast<char>(code);
			}
			else if(code < 0x800)
			{
				s += static_cast<char>(0xC0 | (code >> 6));
				s += static_cast<char>(0x80 | (code & 0x3F));
			}
			else if(code < 0x10000)
			{
				s += static_cast<char>(0xE0 | (code >> 12));
				s += static_cast<char>(0x80 | ((code >> 6) & 0x3F));
				s += static_cast<char>(0x80 | (code & 0x3F));
			}
			else
			{
				s += static_cast<char>(0xF0 | (code >> 18));
				s += static_cast<char>(0x80 | ((code >> 12) & 0x3F));
				s += static_cast<char>(0x80 | ((code >> 6) & 0x3F));
				s += static_cast<char>(0x80 | (code & 0x3F));
			}		
		}

		//le_or_be, true = le, false = be
		void put_utf16char(std::string& s, unsigned long code, bool le_or_be)
		{
			if(code <= 0xFFFF)
			{
				if(le_or_be)
				{
					s += static_cast<char>(code & 0xFF);
					s += static_cast<char>((code & 0xFF00) >> 8);
				}
				else
				{
					s += static_cast<char>((code & 0xFF00) >> 8);
					s += static_cast<char>(code & 0xFF);
				}
			}
			else
			{
				unsigned long ch0 = (0xD800 | ((code - 0x10000) >> 10));
				unsigned long ch1 = (0xDC00 | ((code - 0x10000) & 0x3FF));

				if(le_or_be)
				{
					s += static_cast<char>(ch0 & 0xFF);
					s += static_cast<char>((ch0 & 0xFF00) >> 8);

					s += static_cast<char>(ch1 & 0xFF);
					s += static_cast<char>((ch1 & 0xFF00) >> 8);
				}
				else
				{
					s += static_cast<char>((ch0 & 0xFF00) >> 8);
					s += static_cast<char>(ch0 & 0xFF);

					s += static_cast<char>((ch1 & 0xFF00) >> 8);
					s += static_cast<char>(ch1 & 0xFF);
				}
			}
		}

		void put_utf32char(std::string& s, unsigned long code, bool le_or_be)
		{
			if(le_or_be)
			{
				s += static_cast<char>(code & 0xFF);
				s += static_cast<char>((code & 0xFF00) >> 8);
				s += static_cast<char>((code & 0xFF0000) >> 18);
				s += static_cast<char>((code & 0xFF000000) >> 24);
			}
			else
			{
				s += static_cast<char>((code & 0xFF000000) >> 24);
				s += static_cast<char>((code & 0xFF0000) >> 18);
				s += static_cast<char>((code & 0xFF00) >> 8);
				s += static_cast<char>(code & 0xFF);
			}
		}

		std::string utf8_to_utf16(const std::string& s, bool le_or_be)
		{
			const unsigned char * bytes = reinterpret_cast<const unsigned char*>(s.c_str());
			const unsigned char * end = bytes + s.size();

			std::string utf16str;

			//If there is a BOM, ignore it.
			if(s.size() >= 3)
			{
				if(bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
				{
					bytes += 3;
					put_utf16char(utf16str, 0xFEFF, le_or_be);
				}
			}

			while(bytes != end)
			{
				put_utf16char(utf16str, utf8char(bytes, end), le_or_be);
			}
			return utf16str;
		}

		std::string utf8_to_utf32(const std::string& s, bool le_or_be)
		{
			const unsigned char * bytes = reinterpret_cast<const unsigned char*>(s.c_str());
			const unsigned char * end = bytes + s.size();

			std::string utf32str;
			//If there is a BOM, ignore it.
			if(s.size() >= 3)
			{
				if(bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
				{
					bytes += 3;
					put_utf32char(utf32str, 0xFEFF, le_or_be);
				}
			}

			while(bytes != end)
			{
				put_utf32char(utf32str, utf8char(bytes, end), le_or_be);
			}
			return utf32str;
		}

		std::string utf16_to_utf8(const std::string& s)
		{
			const unsigned char * bytes = reinterpret_cast<const unsigned char*>(s.c_str());
			const unsigned char * end = bytes + s.size();
			bool le_or_be = true;
			std::string utf8str;
			//If there is a BOM, ignore it
			if(s.size() >= 2)
			{
				if(bytes[0] == 0xFF && bytes[1] == 0xFE)
				{
					bytes += 2;
					le_or_be = true;

					utf8str += (char)0xEF;
					utf8str += (char)0xBB;
					utf8str += (char)0xBF;
				}
				else if(bytes[0] == 0xFE && bytes[1] == 0xFF)
				{
					bytes += 2;
					le_or_be = false;
					utf8str += (char)(0xEF);
					utf8str += (char)(0xBB);
					utf8str += (char)(0xBF);
				}
			}

			while(bytes != end)
			{
				put_utf8char(utf8str, utf16char(bytes, end, le_or_be));
			}
			return utf8str;
		}

		std::string utf16_to_utf32(const std::string& s)
		{
			const unsigned char * bytes = reinterpret_cast<const unsigned char*>(s.c_str());
			const unsigned char * end = bytes + s.size();
			bool le_or_be = true;
			std::string utf32str;
			//If there is a BOM, ignore it
			if(s.size() >= 2)
			{
				if(bytes[0] == 0xFF && bytes[1] == 0xFE)
				{
					bytes += 2;
					le_or_be = true;
					put_utf32char(utf32str, 0xFEFF, true);
				}
				else if(bytes[0] == 0xFE && bytes[1] == 0xFF)
				{
					bytes += 2;
					le_or_be = false;
					put_utf32char(utf32str, 0xFEFF, false);
				}
			}

			while(bytes != end)
			{
				put_utf32char(utf32str, utf16char(bytes, end, le_or_be), le_or_be);
			}
			return utf32str;
		}

		std::string utf32_to_utf8(const std::string& s)
		{
			const unsigned char * bytes = reinterpret_cast<const unsigned char*>(s.c_str());
			const unsigned char * end = bytes + (s.size() & (~4 + 1));

			std::string utf8str;
			bool le_or_be = true;
			//If there is a BOM, ignore it
			if(s.size() >= 4)
			{
				if(bytes[0] == 0 && bytes[1] == 0 && bytes[2] == 0xFE && bytes[3] == 0xFF)
				{
					le_or_be = false;
					bytes += 4;
					utf8str += (char)0xEF;
					utf8str += (char)0xBB;
					utf8str += (char)0xBF;
				}
				else if(bytes[0] == 0xFF && bytes[1] == 0xFE && bytes[2] == 0 && bytes[3] == 0)
				{
					le_or_be = true;
					bytes += 4;
					utf8str += (char)0xEF;
					utf8str += (char)0xBB;
					utf8str += (char)0xBF;
				}
			}

			while(bytes < end)
			{
				put_utf8char(utf8str, utf32char(bytes, end, le_or_be));
			}
			return utf8str;
		}

		std::string utf32_to_utf16(const std::string& s)
		{
			const unsigned char * bytes = reinterpret_cast<const unsigned char*>(s.c_str());
			const unsigned char * end = bytes + (s.size() & (~4 + 1));

			std::string utf16str;
			bool le_or_be = true;
			//If there is a BOM, ignore it
			if(s.size() >= 4)
			{
				if(bytes[0] == 0 && bytes[1] == 0 && bytes[2] == 0xFE && bytes[3] == 0xFF)
				{
					le_or_be = false;
					bytes += 4;
					put_utf16char(utf16str, 0xFEFF, false);
				}
				else if(bytes[0] == 0xFF && bytes[1] == 0xFE && bytes[2] == 0 && bytes[3] == 0)
				{
					le_or_be = true;
					bytes += 4;
					put_utf16char(utf16str, 0xFEFF, true);
				}
			}

			while(bytes < end)
			{
				put_utf16char(utf16str, utf32char(bytes, end, le_or_be), le_or_be);
			}
			return utf16str;
		}

		class charset_encoding_interface
		{
		public:
			virtual ~charset_encoding_interface(){}

			virtual charset_encoding_interface * clone() const = 0;

			virtual std::string str() const = 0;
			virtual std::string str(unicode::t) const = 0;
			virtual std::wstring wstr() const = 0;
		};

		class charset_string
			: public charset_encoding_interface
		{
		public:
			charset_string(const std::string& s)
				: data_(s), is_unicode_(false)
			{}


			charset_string(const std::string& s, unicode::t encoding)
				: data_(s), is_unicode_(true), utf_x_(encoding)
			{}
		private:
			virtual charset_encoding_interface * clone() const
			{
				return new charset_string(*this);
			}

			virtual std::string str() const
			{
				if(is_unicode_)
				{
					std::string strbuf;
					switch(utf_x_)
					{
					case unicode::utf8:
#if defined(NANA_WINDOWS)
						strbuf = detail::utf8_to_utf16(data_, true);
						detail::put_utf16char(strbuf, 0, true);
#else
						strbuf = detail::utf8_to_utf32(data_, true);
						detail::put_utf32char(strbuf, 0, true);
#endif
						break;
					case unicode::utf16:
#if defined(NANA_WINDOWS)
						strbuf = data_;
						detail::put_utf16char(strbuf, 0, true);
#else
						strbuf = detail::utf16_to_utf32(data_);
						detail::put_utf32char(strbuf, 0, true);
#endif
						break;
					case unicode::utf32:
#if defined(NANA_WINDOWS)
						strbuf = detail::utf32_to_utf16(data_);
						detail::put_utf16char(strbuf, 0, true);
#else
						strbuf = data_;
						detail::put_utf32char(strbuf, 0, true);
#endif
						break;
					}

					std::string mbstr;
					wc2mb(mbstr, reinterpret_cast<const wchar_t*>(strbuf.c_str()));
					return mbstr;
				}
				return data_;
			}

			virtual std::string str(unicode::t encoding) const
			{
				if(is_unicode_ && (utf_x_ != encoding))
				{
					switch(utf_x_)
					{
					case unicode::utf8:
						switch(encoding)
						{
						case unicode::utf16:
							return detail::utf8_to_utf16(data_, true);
						case unicode::utf32:
							return detail::utf8_to_utf32(data_, true);
						default:
							break;
						}
						break;
					case unicode::utf16:
						switch(encoding)
						{
						case unicode::utf8:
							return detail::utf16_to_utf8(data_);
						case unicode::utf32:
							return detail::utf16_to_utf32(data_);
						default:
							break;
						}
						break;
					case unicode::utf32:
						switch(encoding)
						{
						case unicode::utf8:
							return detail::utf32_to_utf8(data_);
						case unicode::utf16:
							return detail::utf32_to_utf16(data_);
						default:
							break;
						}
						break;
					}
					return std::string();
				}

				std::string wcstr;
				if(mb2wc(wcstr, data_.c_str()))
				{
#if defined(NANA_WINDOWS)
					switch(encoding)
					{
					case unicode::utf8:
						return utf16_to_utf8(wcstr);
					case unicode::utf16:
						return wcstr;
					case unicode::utf32:
						return utf16_to_utf32(wcstr);
					}
#else
					switch(encoding)
					{
					case unicode::utf8:
						return utf32_to_utf8(wcstr);
					case unicode::utf16:
						return utf32_to_utf16(wcstr);
					case unicode::utf32:
						return wcstr;
					}
#endif
				}
				return std::string();
			}

			virtual std::wstring wstr() const
			{
				if(is_unicode_)
				{
					std::string bytes;
#if defined(NANA_WINDOWS)
					switch(utf_x_)
					{
					case unicode::utf8:
						bytes = detail::utf8_to_utf16(data_, true);
						break;
					case unicode::utf16:
						bytes = data_;
						break;
					case unicode::utf32:
						bytes = detail::utf32_to_utf16(data_);
						break;
					}
#else
					switch(utf_x_)
					{
					case unicode::utf8:
						bytes = detail::utf8_to_utf32(data_, true);
						break;
					case unicode::utf16:
						bytes = detail::utf16_to_utf32(data_);
						break;
					case unicode::utf32:
						bytes = data_;
						break;
					}
#endif
					return std::wstring(reinterpret_cast<const wchar_t*>(bytes.c_str()), bytes.size() / sizeof(wchar_t));
				}

				std::wstring wcstr;
				mb2wc(wcstr, data_.c_str());
				return wcstr;
			}
		private:
			std::string data_;
			bool is_unicode_;
			unicode::t utf_x_;
		};


		class charset_wstring
			: public charset_encoding_interface
		{
		public:
			charset_wstring(const std::wstring& s)
				: data_(s)
			{}

			virtual charset_encoding_interface * clone() const
			{
				return new charset_wstring(*this);
			}

			virtual std::string str() const
			{
				if(data_.size())
				{
					std::string mbstr;
					if(wc2mb(mbstr, data_.c_str()))
						return mbstr;
				}
				return std::string();
			}

			virtual std::string str(unicode::t encoding) const
			{
				switch(encoding)
				{
				case unicode::utf8:
#if defined(NANA_WINDOWS)
					return detail::utf16_to_utf8(std::string(reinterpret_cast<const char*>(data_.c_str()), data_.size() * sizeof(wchar_t)));
#else
					return detail::utf32_to_utf8(std::string(reinterpret_cast<const char*>(data_.c_str()), data_.size() * sizeof(wchar_t)));
#endif
				case unicode::utf16:
#if defined(NANA_WINDOWS)
					return std::string(reinterpret_cast<const char*>(data_.c_str()), data_.size() * sizeof(wchar_t));
#else
					return detail::utf32_to_utf16(std::string(reinterpret_cast<const char*>(data_.c_str()), data_.size() * sizeof(wchar_t)));
#endif
				case unicode::utf32:
#if defined(NANA_WINDOWS)
					return detail::utf16_to_utf32(std::string(reinterpret_cast<const char*>(data_.c_str()), data_.size() * sizeof(wchar_t)));
#else
					return std::string(reinterpret_cast<const char*>(data_.c_str()), data_.size() * sizeof(wchar_t));
#endif
				}
				return std::string();
			}

			virtual std::wstring wstr() const
			{
				return data_;
			}
		private:
			std::wstring data_;
		};
	}
	//class charset
		charset::charset(const charset& rhs)
			: impl_(rhs.impl_ ? rhs.impl_->clone() : 0)
		{}

		charset & charset::operator=(const charset& rhs)
		{
			if(this != &rhs)
			{
				delete impl_;
				impl_ = (rhs.impl_ ? rhs.impl_->clone() : 0);
			}
			return *this;
		}

		charset::charset(const std::string& s)
			: impl_(new detail::charset_string(s))
		{}

		charset::charset(const std::string& s, unicode::t encoding)
			: impl_(new detail::charset_string(s, encoding))
		{}

		charset::charset(const std::wstring& s)
			: impl_(new detail::charset_wstring(s))
		{}

		charset::~charset()
		{
			delete impl_;
		}

		charset::operator std::string() const
		{
			return impl_->str();
		}

		charset::operator std::wstring() const
		{
			return impl_->wstr();
		}

		std::string charset::to_bytes(unicode::t encoding) const
		{
			return impl_->str(encoding);
		}
	//end class charset

}//end namespace nana
