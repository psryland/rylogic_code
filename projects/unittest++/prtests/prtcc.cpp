//*************************************************************
// Unit Test for prtcc
//*************************************************************
#include "UnitTest++/1.3/Src/UnitTest++.h"
#include "UnitTest++/1.3/Src/ReportAssert.h"
#include <vector>
#include "pr/tcc/prtcc.h"

// Report compiler errors/warnings
void PrintErrors(void*, char const* msg)
{
	printf("\n%s\n",msg);
}

// this function is called by the generated code
int fib(int n)
{
	return (n <= 2) ? 1 : fib(n-1) + fib(n-2);
}

SUITE(PRTcc)
{
	#ifndef _WIN64
	TEST(One)
	{
		char my_program[] =
			"int doit(int i) { return fib(i); }\n"
			"int main(int argc, char* argv[])\n"
			"{\n"
			"	return doit(12);\n"
			"}\n"
			;

		{// Compile the program into memory
			// Create an instance of the tcc compiler
			pr::tcc::Compiler tcc(pr::tcc::EOutput::Memory, pr::tcc::EOutputFormat::Bin, PrintErrors, 0, false);
			
			// Add symbols available to the compiled code
			tcc.add_symbol("fib", fib);

			//typedef int (*Entry)(int);
			typedef pr::tcc::Program<int(*)(int)> MyProgram;
			MyProgram program;
			
			tcc.build(my_program, "doit", program);
			int result0 = program.run(12);
			int RESULT0 = fib(12);
			CHECK(result0 == RESULT0);
		}

		{// Execute the program
			// Create an instance of the tcc compiler
			pr::tcc::Compiler tcc(pr::tcc::EOutput::Memory, pr::tcc::EOutputFormat::Bin, PrintErrors, 0, false);
			
			// Add symbols available to the compiled code
			tcc.add_symbol("fib", fib);

			int result0 = tcc.run(my_program);
			int RESULT0 = fib(12);
			CHECK(result0 == RESULT0);
		}
	}
	#endif
}