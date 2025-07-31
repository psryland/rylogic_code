#if 1
#include "pr/common/min_max_fix.h"
#include "pr/network/socket_stream.h"
#include "pr/view3d-12/ldraw/ldraw_builder.h"

namespace tests
{
	void Run()
	{
		pr::rdr12::ldraw::Builder builder;
		builder.Group("g", 0xFFFF0000).Box("b", 0xFF00FF00).dim(1, 2, 3);

		pr::network::Winsock winsock;
		pr::network::socket_stream ldr;
		
		if (ldr.connect("localhost", 1976).good())
			ldr << builder.ToText(false) << std::flush;

		//std::cout << builder.ToString(true);
		//ldr << "*Box bb FF00FF00 { *Data {1 2 3} }";
		//ldr.flush();

	}
}
#endif
