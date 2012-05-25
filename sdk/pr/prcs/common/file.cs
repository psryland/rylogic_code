//***********************************************
// File
//	(c)opyright Paul Ryland 2008
//***********************************************

using System;
using System.IO;

namespace PR
{
	static class File
	{
		/// <summary>
		/// Reads data from a stream until the end is reached.
		/// The data is returned as a byte array.
		/// An IOException is thrown if any of the underlying IO calls fail.
		/// </summary>
		/// <param name="stream">The stream to read data from</param>
		/// <param name="size_estimate">The initial buffer length</param>
		public static byte[] StreamToBuffer(Stream stream, int size_estimate)
		{
			// If we've been passed an unhelpful initial length, just use 32K.
			size_estimate = size_estimate > 0 ? size_estimate : 32768;
			
			byte[] buffer = new byte[size_estimate];
			
			int read = 0, chunk = 0;
			while( (chunk = stream.Read(buffer, read, buffer.Length - read)) > 0 )
			{
				read += chunk;

				// If we've reached the end of our buffer,
				// check to see if there's any more data.
				if( read == buffer.Length )
				{
					// Look for the end of the stream
					int next_byte = stream.ReadByte();
					if( next_byte == -1 ) return buffer;

					// Grow the buffer
					byte[] buf = new byte[buffer.Length * 3 / 2];
					Array.Copy(buffer, buf, buffer.Length);
					buffer[read++] = (byte)next_byte;
					buffer = buf;
				}
			}
			
			// Shrink the buffer to fit the data
			byte[] ret = new byte[read];
			Array.Copy(buffer, ret, read);
			return ret;
		}
		public static byte[] StreamToBuffer(Stream stream)
		{
			return StreamToBuffer(stream, 0);
		}


		///// <summary>
		///// Reads text data from a stream until the end is reached.
		///// The data is returned as a string.
		///// An IOException is thrown if any of the underlying IO calls fail.
		///// </summary>
		///// <param name="stream">The stream to read data from</param>
		///// <param name="size_estimate">The initial string length</param>
		//public static string StreamToString(Stream stream, int size_estimate)
		//{
		//    // If we've been passed an unhelpful initial length, just use 32K.
		//    size_estimate = size_estimate > 0 ? size_estimate : 32768;

		//    string str;
		//    str.
		//    byte[] buffer = new byte[size_estimate];

		//    int read = 0, chunk = 0;
		//    while ((chunk = stream..Read(buffer, read, buffer.Length - read)) > 0)
		//    {
		//        read += chunk;

		//        // If we've reached the end of our buffer,
		//        // check to see if there's any more data.
		//        if (read == buffer.Length)
		//        {
		//            // Look for the end of the stream
		//            int next_byte = stream.ReadByte();
		//            if (next_byte == -1) return buffer;

		//            // Grow the buffer
		//            byte[] buf = new byte[buffer.Length * 3 / 2];
		//            Array.Copy(buffer, buf, buffer.Length);
		//            buffer[read++] = (byte)next_byte;
		//            buffer = buf;
		//        }
		//    }

		//    // Strink the buffer to fit the data
		//    byte[] ret = new byte[read];
		//    Array.Copy(buffer, ret, read);
		//    return ret;
		//}
		//public static string StreamToString(Stream stream)
		//{
		//    return StreamToBuffer(stream, 0);
		//}
	}
}
