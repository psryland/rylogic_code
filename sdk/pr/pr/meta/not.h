#ifndef PR_META_NOT_H
#define PR_META_NOT_H

namespace pr
{
	namespace mpl
	{
		template <typename T>
		struct not_
		{
			enum { value = !T::value };
		};

	}//namespace mpl
}//namespace pr

#endif//PR_META_NOT_H
