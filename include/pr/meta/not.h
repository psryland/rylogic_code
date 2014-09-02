#ifndef PR_META_NOT_H
#define PR_META_NOT_H

namespace pr
{
	namespace meta
	{
		template <typename T>
		struct not_
		{
			enum { value = !T::value };
		};
	}
}

#endif
