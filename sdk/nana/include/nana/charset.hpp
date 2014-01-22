#ifndef NANA_CHARSET_HPP
#define NANA_CHARSET_HPP
#include <string>

namespace nana
{
	struct unicode
	{
		enum t{utf8, utf16, utf32};
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

		charset(const std::string&);
		charset(const std::string&, unicode::t);
		charset(const std::wstring&);

		~charset();

		operator std::string() const;
		operator std::wstring() const;
		std::string to_bytes(unicode::t) const;
	private:
		detail::charset_encoding_interface* impl_;
	};

}//end namespace nana
#endif
