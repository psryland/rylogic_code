#ifndef PR_META_CONSTANTS_H
#define PR_META_CONSTANTS_H

namespace pr
{
	namespace meta
	{
		struct true_  { enum { value = true  }; char s[2]; };
		struct false_ { enum { value = false }; char s[1]; };

		template <bool b>
		struct bool_ { enum { value = b }; };
	}
}

#endif
