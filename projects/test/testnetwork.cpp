//************************************************************************
//************************************************************************
#include "test.h"
#include "pr/common/hresult.h"
#include "pr/network/network.h"

namespace TestNetwork
{
	using namespace pr;

	void TestAll();

	void Run()
	{
		pr::network::Winsock winsock;
		pr::network::Server server(winsock);
		pr::network::Client client(winsock);

		server.AllowConnections(4000, IPPROTO_TCP, 1);
		client.Connect(IPPROTO_TCP, "127.0.0.1", 4000);

		char const ping[] = "ping\0";
		char const pong[] = "pong\0";
		char buf[10];
		size_t bytes_read;
		while ((GetAsyncKeyState(VK_ESCAPE)&0x8000) == 0)
		{
			if (server.Recv(buf, sizeof(buf), bytes_read, 1000) && bytes_read != 0) printf("Serv: %s\n", buf);
			server.Send(ping, sizeof(ping), 1000);
			if (client.Recv(buf, sizeof(buf), bytes_read, 1000) && bytes_read != 0) printf("Recv: %s\n", buf);
			client.Send(pong, sizeof(pong), 1000);
		}
	}
}//namespace TestNetwork