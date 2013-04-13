// Use Show Preprocessed on this file
#include "pr\macros\enum.h"

#define PR_ENUM(x) \
  x(A, = 42)\
  x(B, = 43) /* this is 'B' */ \
  x(C, = 44)
PR_DECLARE_ENUM(Blah, PR_ENUM);
#undef PR_ENUM
