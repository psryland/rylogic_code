//*****************************************
//*****************************************
#include "test.h"
#include "pr/common/fmt.h"
#include "pr/threads/mutex.h"
#include "pr/threads/ipc.h"

namespace TestIPC
{
	using namespace pr;

	struct Msg
	{
		char str[256];
	};
	void Run()
	{
		IPCMessage ipc("TestIPCMessage", sizeof(Msg));
		for( int i = 0; i != 10; ++i )
		{
			switch( ipc.m_ipc.GetRole() )
			{
			case ipc::Client: printf("Client:\n"); break;
			case ipc::Server: printf("Server:\n"); break;
			case ipc::Unknown: printf("Unknown:\n"); break;
			}
			printf("%d> ", i);
			Msg msg;
			fgets(msg.str, 256, stdin);
			ipc.Send(msg, 0, 3000);

			while( ipc.Recv(msg, 3000) )
			{
				printf("<< %s\n", msg.str);
			}
		}
	}

	//void Run()
	//{
	//	InterProcessCommunicator ipc;
	//	if( !ipc.Initialise("Test_IPC", 1024) )
	//	{
	//		printf("Initalise failed\n");
	//		return;
	//	}

	//	for( int i = 0; i < 10; ++i )
	//	{
	//		printf("Connection attempt: %d...", i);	
	//		if( ipc.Connect(1000) ) { printf("Success!\n"); break; }
	//		printf("Failed\n");	
	//	}
	//	if( !ipc.IsConnected() )
	//	{
	//		printf("Giving up.\n");	
	//		return;
	//	}

	//	std::string str;
	//	for( int i = 0; i < 10; ++i )
	//	{
	//		str = Fmt("Sending: %d\n", i);
	//		ipc.Send(str.c_str(), (uint)str.length() + 1);
	//		
	//		char buffer[256];
	//		DWORD bytes_read = ipc.Receive(buffer, 255, 2000L);
	//		buffer[bytes_read] = '\0';
	//		printf("Received: %s\n", buffer);
	//	}

	//}

}//namespace TestIPC
