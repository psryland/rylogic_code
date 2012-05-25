#include "unittest++/1.3/src/unittest++.h"
#include "AssertException.h"

namespace UnitTest {

void ReportAssert(char const* description, char const* filename, int const lineNumber)
{
    throw AssertException(description, filename, lineNumber);
}

}
