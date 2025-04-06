#! "net9.0"
//#r "nuget: System.Text.Json, 9.0.3"
//#r "nuget: Rylogic.Core, 1.0.4"
//#r "nuget: Rylogic.Gfx, 1.0.4"
#r "E:\Rylogic\Code\projects\rylogic\Rylogic.Core\bin\Debug\net9.0-windows\Rylogic.Core.dll"
#r "E:\Rylogic\Code\projects\rylogic\Rylogic.Gfx\bin\Debug\net9.0-windows\Rylogic.Gfx.dll"

using System.Threading;
using System.Net.Sockets;
using Rylogic.Maths;
using Rylogic.LDraw;

{
	// Open a TCP connection to localhost:1976
	using var client = new TcpClient("localhost", 1976);
	using var stream = client.GetStream();

	// Switch the stream to binary mode
	var builder = new Builder();
	builder.BinaryStream();
	builder.ToText().CopyTo(stream);
	stream.Flush();

	// Create a box and write it to the stream
	{
		builder.Reset();
		builder.Box("B", 0xFF00FF00).dim(1).pos(0, 0, 0);
		builder.Command().add_to_scene(0);
		builder.ToBinary().CopyTo(stream);
		stream.Flush();
	}

	for (int i = 0; ; ++i)
	{
		builder.Reset();
		builder.Command().object_transform("B", m4x4.Transform(v4.ZAxis, i * 0.1f, v4.Origin));
		builder.ToBinary().CopyTo(stream);
		stream.Flush();

		Thread.Sleep(10);
	}
}