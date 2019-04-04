using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Threading;
using Rylogic.Common;

namespace Rylogic.Utility
{
	public static class FileContent
	{
		/// <summary>Starts a worker thread to read bytes from a memory mapped file</summary>
		public static ManualResetEventSlim BeginRead(string filepath, WaitHandle stop, Action<ArraySegment<byte>> data_cb, int block_size = 1024)
		{
			// Use:
			//  var stop = new ManualResetEventSlim(false);
			//  var done = FileContent.BeginRead(filepath, stop, s => UseBytes(s));
			//  ...
			//  stop.Set();
			//  done.WaitOne();

			var done = new ManualResetEventSlim(false);
			ThreadPool.QueueUserWorkItem(async _ =>
			{
				var sr = (Stream)null;
				var mmf = (MemoryMappedFile)null;
				try
				{
					var buffer = new byte[block_size];
					for (; ; Thread.Yield())
					{
						// Create the memory mapped file after the monitored file is created
						if (sr == null)
						{
							try
							{
								// Cancelled already?
								if (stop.WaitOne(0))
									break;

								// Wait for the file to appear.
								// 'MemoryMappedFile.CreateFromFile' fails for null or zero-length files
								if (!Path_.FileExists(filepath) || Path_.FileLength(filepath) == 0)
								{
									Thread.Sleep(100);
									continue;
								}

								// Once the file exists, create a memory mapped file from it
								mmf = mmf ?? MemoryMappedFile.CreateFromFile(filepath, FileMode.Open, Path_.SanitiseFileName($"FileContent-{filepath}"), 0, MemoryMappedFileAccess.Read);
								sr = mmf.CreateViewStream();
							}
							catch (UnauthorizedAccessException)
							{
								Thread.Sleep(100);
								continue;
							}
						}

						// Read blocks of data from the file
						var read = await sr.ReadAsync(buffer, 0, buffer.Length);
						if (read != 0)
						{
							data_cb(new ArraySegment<byte>(buffer, 0, read));
							continue;
						}

						// Only test for exit if no data was read
						if (stop.WaitOne(0))
							break;

						// If no data was read from the file, wait a bit longer
						Thread.Sleep(10);
					}
				}
				finally
				{
					sr?.Dispose();
					mmf?.Dispose();
					done.Set();
				}
			});
			return done;
		}

		/// <summary>Starts a worker thread to read lines from the file</summary>
		public static ManualResetEventSlim BeginReadLines(string filepath, WaitHandle stop, Action<string> lines_cb, Encoding encoding = null)
		{
			// Use:
			//  var stop = new ManualResetEventSlim(false);
			//  var done = FileContent.BeginReadLines(filepath, stop, s => UseLine(s));
			//  ...
			//  stop.Set();
			//  done.WaitOne();

			var done = new ManualResetEventSlim(false);
			ThreadPool.QueueUserWorkItem(async _ =>
			{
				var sr = (StreamReader)null;
				var mmf = (MemoryMappedFile)null;
				try
				{
					for (; ; Thread.Yield())
					{
						// Create the memory mapped file after the monitored file is created
						if (sr == null)
						{
							// Cancelled already?
							if (stop.WaitOne(0))
								break;

							// Wait for the file to appear.
							// 'MemoryMappedFile.CreateFromFile' fails for null or zero-length files
							if (!Path_.FileExists(filepath) || Path_.FileLength(filepath) == 0)
							{
								Thread.Sleep(100);
								continue;
							}

							// Once the file exists, create a memory mapped file from it
							mmf = mmf ?? MemoryMappedFile.CreateFromFile(filepath, FileMode.Open, Path_.SanitiseFileName($"FileContent-{filepath}"), 0, MemoryMappedFileAccess.Read);
							sr = new StreamReader(mmf.CreateViewStream(), encoding ?? Encoding.UTF8);
						}

						// Read lines from the file
						var line = await sr.ReadLineAsync();
						if (line != null)
						{
							lines_cb(line.Trim('\0'));
							continue;
						}

						// Only test for exit if no data was read
						if (stop.WaitOne(0))
							break;

						// If no data was read from the file, wait a bit longer
						Thread.Sleep(10);
					}
				}
				finally
				{
					sr?.Dispose();
					mmf?.Dispose();
					done.Set();
				}
			});
			return done;
		}
	}
}
