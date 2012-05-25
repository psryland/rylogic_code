#include "unittest++/1.3/src/unittest++.h"
#include "DeferredTestResult.h"

#include <cstdlib>

namespace UnitTest
{

DeferredTestResult::DeferredTestResult()
	: suiteName("")
	, testName("")
	, failureFile("")
	, timeElapsed(0.0f)
	, failed(false)
{
}

DeferredTestResult::DeferredTestResult(char const* suite, char const* test)
	: suiteName(suite)
	, testName(test)
	, failureFile("")
	, timeElapsed(0.0f)
	, failed(false)
{
}

}
