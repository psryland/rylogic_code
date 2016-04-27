#pragma once

#if _MSC_VER < 1900
#  ifndef alignas
#    define alignas(alignment) __declspec(align(alignment))
#  endif
#endif

