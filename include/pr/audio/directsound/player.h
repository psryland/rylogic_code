//*********************************************
// Sound IDecoder
//  Copyright (c) Rylogic Ltd 2007
//*********************************************
// Notes:
//   - DirectSound8 is deprecated, prefer XAudio2

#pragma once
#include <fstream>
#include <filesystem>
#include "pr/common/cast.h"
#include "pr/audio/directsound/sound.h"

namespace pr::sound
{
	// Interface to a data stream
	struct IDataStream
	{
		virtual ~IDataStream() {}
			
		// Read bytes from the stream and copies them into 'ptr'. Same inputs/outputs as fread
		virtual size_t IDataStream_Read(void* ptr, size_t byte_size, size_t size_to_read) = 0;
			
		// Seek to a position in the input stream. Same inputs/outputs as fseek.
		// Seek is optional, if seeking is not supported, return -1
		virtual int IDataStream_Seek(long offset, int seek_from) = 0;
			
		// Return the byte position of the next byte in the data stream that would be read. Same inputs/outputs as ftell.
		virtual long IDataStream_Tell() const = 0;
			
		// Closes the data stream. Same as fclose.
		virtual void IDataStream_Close() = 0;
	};
		
	// A type that plays a sound and manages filling a dsound buffer from a data stream
	struct Player
	{
		// Notes:
		//  - A Player lives for the duration of a sound being played. For long running sounds (i.e. infinite loops)
		//    the application main loop needs to periodically call 'Update' to keep the DX sound buffer filled.

		D3DPtr<IDirectSoundBuffer8> m_buf; // The buffer this player is filling
		IDataStream* m_src;                // The source of data
		size_t m_buf_size;                 // The size of the buffer pointed to by 'm_buf'
		size_t m_pos;                      // The position we're writing to in 'm_buf'
		float m_volume;                    // The playback volume
		bool m_src_end;                    // True after we've read the last byte from the source (implies !m_loop)
		bool m_loop;                       // Loop the sample
			
		Player()
			:m_buf()
			,m_src()
			,m_buf_size()
			,m_pos(0)
			,m_volume(0.5f)
			,m_src_end(false)
			,m_loop(false)
		{}
		~Player()
		{
			if (m_src)
				m_src->IDataStream_Close();
		}

		// Set this decoder to copy data from 'src' to 'buf'
		// 'src' may be nullptr, in which case 'buf' will be filled with zeros
		// 'buf' may be nullptr to release the reference held by 'm_buf'
		// Looping is handled by the IDataStream. It should wrap internally giving the impression of an infinitely long buffer
		void Set(IDataStream* src, D3DPtr<IDirectSoundBuffer8>& buf)
		{
			if (m_src) m_src->IDataStream_Close();
			m_src = src;
			m_buf = buf;
			m_buf_size = GetBufferSize(buf);
			SetVolume(m_volume);
			Throw(m_buf->SetCurrentPosition(0));
			Update(true);
		}

		// Returns true while this player is playing
		bool IsPlaying() const
		{
			if (!m_buf)
				return false;

			DWORD status;
			Throw(m_buf->GetStatus(&status));
			return (status & DSBSTATUS_PLAYING) != 0;
		}

		// Set the playback volume
		void SetVolume(float vol)
		{
			m_volume = vol;
			if (m_buf)
				sound::SetVolume(m_buf, vol);
		}
			
		// Start the sample playing
		void Play(bool loop, DWORD priority = 0)
		{
			// The dsound buffer is played as looping because its size is independent of the src data size.
			// For non-looped sounds we will call stop during Update() after all data has been read from the stream.
			PR_ASSERT(PR_DBG_SND, m_buf, "");
			Throw(m_buf->Play(0, priority, DSBPLAY_LOOPING));
			m_src_end = false;
			m_loop = loop;
		}
			
		// Stop the sample playing
		// Note: no Rewind() or SetPosition() in the player as that can be
		// done in the source stream which knows if it's seekable or not.
		void Stop()
		{
			if (!m_buf) return;
			Throw(m_buf->Stop());
		}

		// Transfers more data from the source stream into the dsound buffer
		void Update(bool force = false)
		{
			// This method should be called when the Sound raises the update event

			// Only update if the sound is playing (under normal conditions)
			if (!m_buf || !(force || IsPlaying()))
				return;
				
			// Get the read/write positions in the dsound buffer and the space that is available for filling
			// Note: wpos here is the next byte that can be written, not where we last finished writing to.
			DWORD rpos;
			pr::Throw(m_buf->GetCurrentPosition(&rpos, 0));
			size_t ahead = (m_pos - rpos + m_buf_size) % m_buf_size; // This is how far ahead of the read position our write position is
				
			// If we've reached the end of the source, and 'rpos' has moved past 'm_pos'
			// then 'ahead' will be "negative" and we can stop playback
			if (m_src_end)
			{
				if (ahead > m_buf_size/2) Stop();
				return;
			}
				
			// Only fill the buffer up to half full. This minimises the problems
			// with aliasing and allows us to tell when 'rpos' has overtaken 'm_pos'.
			size_t fill = pr::Clamp<size_t>((m_buf_size/2) - ahead, 0, m_buf_size/2);
			if (fill < m_buf_size/8) return; // wait for a minimum amount to do
				
			// Add more sound data to the writable part of the buffer
			Lock lock(m_buf, m_pos, fill);
			size_t read = Read(lock.m_ptr0, lock.m_size0) + Read(lock.m_ptr1, lock.m_size1);
			m_pos = (m_pos + read) % m_buf_size;
			m_src_end = read == 0;
		}

	private:

		// Read 'count' bytes into 'ptr'. If the source stream returns less than
		// 'count' bytes the remaining bytes in 'ptr' are filled with zeros.
		// Returns the number of bytes read from the source stream.
		size_t Read(uint8_t* ptr, size_t count)
		{
			size_t src_read = 0;
			for (size_t read = 0; count != 0; ptr += read, count -= read, src_read += read)
			{
				read = m_src->IDataStream_Read(ptr, 1, count);
				if (read == 0)
				{
					// If not looping or if no data can be read from the start of the stream then quit
					// otherwse, repeatedly seek to the beginning and reread until we've read 'count' bytes
					if (!m_loop || m_src->IDataStream_Tell() == 0) break;
					else if (m_src->IDataStream_Seek(0, SEEK_SET) == -1) { PR_ASSERT(PR_DBG_SND, false, "Cannot loop as 'm_src' is not seekable"); break; }
				}
			}
			memset(ptr, 0, count); // Fill any remaining space with zeros
			return src_read;
		}
	};
		
	// Some default DataStream implementations

	// A local buffer containing the sound file data
	struct MemDataStream :IDataStream
	{
		std::vector<uint8_t> m_data;
		size_t m_pos;
		bool m_delete_on_close;

		MemDataStream(bool delete_on_close = false)
			: m_data()
			, m_pos(0)
			, m_delete_on_close(delete_on_close)
		{}
		MemDataStream(std::filesystem::path const& filepath, bool delete_on_close = false)
			: m_data()
			, m_pos(0)
			, m_delete_on_close(delete_on_close)
		{
			// Fill 'm_data' from the file
			m_data.resize(s_cast<size_t, true>(std::filesystem::file_size(filepath)));
			std::ifstream file(filepath, std::ios::binary);
			if (file.read(char_ptr(m_data.data()), m_data.size()).gcount() != static_cast<std::streamsize>(m_data.size()))
				throw std::runtime_error(Fmt("Failed to read audio file: '%S'", filepath.c_str()));
		}
		size_t IDataStream_Read(void* ptr, size_t byte_size, size_t size_to_read)
		{
			size_t count = size_to_read * byte_size;
			if (count > m_data.size() - m_pos) count = m_data.size() - m_pos;
			if (count != 0) { memcpy(ptr, &m_data[m_pos], count); m_pos += count; }
			return count;
		}
		int IDataStream_Seek(long offset, int seek_from)
		{
			switch (seek_from)
			{
			case SEEK_SET:
				m_pos = size_t(offset);
				return 0;
			case SEEK_CUR:
				m_pos += size_t(offset);
				return 0;
			case SEEK_END:
				m_pos = m_data.size() - size_t(offset);
				return 0;
			default:
				return -1;
			}
		}
		long IDataStream_Tell() const
		{
			return long(m_pos);
		}
		void IDataStream_Close()
		{
			if (m_delete_on_close)
				delete this;
		}
	};
}
