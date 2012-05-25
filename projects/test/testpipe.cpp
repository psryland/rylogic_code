//*****************************************
//*****************************************
#include "test.h"
#include "pr/common/pipe.h"
#include "pr/threads/critical_section.h"

namespace TestPipe
{
	using namespace pr;

	void OnRecv(void const* data, std::size_t, bool, void*)
	{
		printf("%s", static_cast<char const*>(data));
	}
	
	void Run()
	{
		// Create a pipe. This starts a thread listening for incomming connections
		// When a connection is received,
		//	it's added to a vector of connections
		//	a thread is started to listen for data
		//	the OnRecv function is called for each lot of data
		pr::Pipe<> pipe("LineDrawerListener", OnRecv, 0);
		for(;;)
		{
			char buffer[256];
			fgets(buffer, 256, stdin);
			if( strncmp(buffer, "exit", 4) == 0 ) break;
			pipe.Send(buffer, strlen(buffer));
		}
	}
}//namespace TestPipe
