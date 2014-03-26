#ifndef NANA_CHARSET_HPP
#define NANA_CHARSET_HPP
#include <string>

namespace nana
{
	enum class unicode
	{
		utf8, utf16, utf32
	};

	namespace detail
	{
		class charset_encoding_interface;
	}

	class charset
	{
	public:
		charset(const charset&);
		charset & operator=(const charset&);
		charset(charset&&);
		charset & operator=(charset&&);

		charset(const std::string&);
		charset(std::string&&);
		charset(const std::string&, unicode);
		charset(std::string&&, unicode);
		charset(const std::wstring&);
		charset(std::wstring&&);
		~charset();
		operator std::string() const;
		operator std::string&&();
		operator std::wstring() const;
		operator std::wstring&&();
		std::string to_bytes(unicode) const;
	private:
		detail::charset_encoding_interface* impl_;
	};

}//end namespace nana
#endif
