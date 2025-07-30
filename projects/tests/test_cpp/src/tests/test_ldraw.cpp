#if 1
#include "pr/common/min_max_fix.h"
#include "pr/network/socket_stream.h"
#include "pr/view3d-12/ldraw/ldraw_builder.h"

namespace tests
{
	void Run()
	{
		pr::network::Winsock winsock;
		pr::network::socket_stream ldr("localhost", 1976);

		pr::rdr12::ldraw::Builder builder;
		builder.Group("g", 0xFFFF0000).Box("b", 0xFF00FF00).dim(1, 2, 3);
		std::cout << builder.ToString(true);
	}
}
#endif
