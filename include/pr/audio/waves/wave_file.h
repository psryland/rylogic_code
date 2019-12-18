//***************************************************************************************************
// Audio
//  Copyright (c) Rylogic Ltd 2017
//***************************************************************************************************
#pragma once
#include "pr/audio/forward.h"

namespace pr::audio
{
	// Header for a .wav file
	struct WaveHeader
	{
		static constexpr DWORD RIFF_be = 0x52494646U;
		static constexpr DWORD WAVE_be = 0x57415645U;
		static constexpr DWORD FMT_be = 0x666d7420U;
		static constexpr DWORD DATA_be = 0x64617461U;

		static constexpr DWORD RIFF_le = 0x46464952U;
		static constexpr DWORD WAVE_le = 0x45564157U;
		static constexpr DWORD FMT_le = 0x20746d66U;
		static constexpr DWORD DATA_le = 0x61746164U;

		// File chunk
		DWORD file_chunk_id;    // 0x52494646 "RIFF" in big endian
		DWORD file_size;        // 4 + (8 + fmt_chunk_size) + (8 + data_chunk_size)
		DWORD file_data_format; // 0x57415645 "WAVE" in big endian

		// Format chunk
		DWORD fmt_chunk_id;   // 0x666d7420 "fmt " in big endian
		DWORD fmt_chunk_size; // 16 for PCM
		WORD  audioFormat;   // 1 for PCM
		WORD  numChannels;   // 1 for mono, 2 for stereo
		DWORD sampleRate;    // 8000, 22050, 44100, etc...
		DWORD byteRate;      // sampleRate * numChannels * bitsPerSample/8
		WORD  blockAlign;    // numChannels * bitsPerSample/8
		WORD  bitsPerSample; // number of bits (8 for 8 bits, etc...)

		DWORD data_chunk_id;   // 0x64617461 "data" in big endian
		DWORD data_chunk_size; // numSamples * numChannels * bitsPerSample/8 (this is the actual data size in bytes)
		// data follows

		WaveHeader()
			:file_chunk_id()
			,file_size()
			,file_data_format()
			,fmt_chunk_id()
			,fmt_chunk_size()
			,audioFormat()
			,numChannels()
			,sampleRate()
			,byteRate()
			,blockAlign()
			,bitsPerSample()
			,data_chunk_id()
			,data_chunk_size()
		{}
		WaveHeader(int sample_count, ESampleRate sample_rate, int channels, int bits_per_sample)
		{
			// File type chunk
			file_chunk_id = RIFF_le;
			file_data_format = WAVE_le;

			// Format chunk
			fmt_chunk_id   = FMT_le;                        //0x666d7420;  // "fmt " in big endian
			fmt_chunk_size = 16;                            // 16 for PCM
			audioFormat    = 1;                             // 1 for PCM
			numChannels    = s_cast<WORD>(channels);        // 1 for mono, 2 for stereo
			bitsPerSample  = s_cast<WORD>(bits_per_sample); // number of bits (8 for 8 bits, etc...)
			sampleRate     = s_cast<DWORD>(sample_rate);    // 8000, 22050, 44100, etc...
			byteRate       = sampleRate * numChannels * bitsPerSample / 8;
			blockAlign     = numChannels * bitsPerSample / 8;

			// Data chunk
			data_chunk_id = DATA_le;
			data_chunk_size = sample_count * numChannels * bitsPerSample / 8; // (this is the actual data size in bytes)

			// Total size including header and data
			file_size = 4 + (8 + fmt_chunk_size) + (8 + data_chunk_size);
		}
		WaveHeader(std::chrono::milliseconds duration, ESampleRate sample_rate, int channels, int bits_per_sample)
			:WaveHeader((int)::ceilf(duration.count()* static_cast<int>(sample_rate) * 0.001f), sample_rate, channels, bits_per_sample)
		{}
	};

	// Wave data info
	struct WavData
	{
		WAVEFORMATEX const* wfx;
		uint8_t const* audio_start;
		uint32_t audio_bytes;
		uint32_t loop_start;
		uint32_t loop_length;
		uint32_t const* seek;       // Note: XMA Seek data is Big-Endian
		uint32_t seek_count;
	};

	// Constants
	static int const MSADPCM_HEADER_LENGTH = 7;
	static int const MSADPCM_FORMAT_EXTRA_BYTES = 32;
	static int const MSADPCM_BITS_PER_SAMPLE = 4;
	static int const MSADPCM_MIN_SAMPLES_PER_BLOCK = 4;
	static int const MSADPCM_MAX_SAMPLES_PER_BLOCK = 64000;
	static int const MSADPCM_NUM_COEFFICIENTS = 7;
	static int const XMA_OUTPUT_SAMPLE_BITS = 16;

	// Load and parse a wave file from data in memory
	void LoadWAVAudioInMemory(uint8_t const* wav_data, size_t wav_data_size, WAVEFORMATEX const*& wfx, uint8_t const*& audio_start, uint32_t& audio_bytes);
	void LoadWAVAudioInMemory(uint8_t const* wav_data, size_t wav_data_size, WavData& result);

	// Load and parse a wave file from a file
	void LoadWAVAudioFromFile(std::filesystem::path const& filepath, std::unique_ptr<uint8_t[]>& wav_data, WAVEFORMATEX const*& wfx, uint8_t const*& audio_start, uint32_t& audio_bytes);
	void LoadWAVAudioFromFile(std::filesystem::path const& filepath, std::unique_ptr<uint8_t[]>& wav_data, WavData& result);

	// Return the string for a wave file tag
	inline char const* FormatTagName(WORD wFormatTag)
	{
		switch (wFormatTag)
		{
		case WAVE_FORMAT_PCM:             return "PCM";
		case WAVE_FORMAT_ADPCM:           return "MS ADPCM";
		case WAVE_FORMAT_EXTENSIBLE:      return "EXTENSIBLE";
		case WAVE_FORMAT_IEEE_FLOAT:      return "IEEE float";
		case WAVE_FORMAT_MPEGLAYER3:      return "ISO/MPEG Layer3";
		case WAVE_FORMAT_DOLBY_AC3_SPDIF: return "Dolby Audio Codec 3 over S/PDIF";
		case WAVE_FORMAT_WMAUDIO2:        return "Windows Media Audio";
		case WAVE_FORMAT_WMAUDIO3:        return "Windows Media Audio Pro";
		case WAVE_FORMAT_WMASPDIF:        return "Windows Media Audio over S/PDIF";
		case 0x165: /*WAVE_FORMAT_XMA*/   return "XBox XMA";
		case 0x166: /*WAVE_FORMAT_XMA2*/  return "XBox XMA2";
		default:                          return "*UNKNOWN*";
		}
	}

	// Convert a channel mask to a string description
	inline char const* ChannelDesc(DWORD dwChannelMask)
	{
		switch (dwChannelMask)
		{
		default: return "Custom";
		case 0x00000004: return "Mono";        // SPEAKER_MONO            
		case 0x00000003: return "Stereo";      // SPEAKER_STEREO          
		case 0x0000000B: return "2.1";         // SPEAKER_2POINT1         
		case 0x00000107: return "Surround";    // SPEAKER_SURROUND        
		case 0x00000033: return "Quad";        // SPEAKER_QUAD            
		case 0x0000003B: return "4.1";         // SPEAKER_4POINT1         
		case 0x0000003F: return "5.1";         // SPEAKER_5POINT1         
		case 0x000000FF: return "7.1";         // SPEAKER_7POINT1         
		case 0x0000060F: return "Surround5.1"; // SPEAKER_5POINT1_SURROUND
		case 0x0000063F: return "Surround7.1"; // SPEAKER_7POINT1_SURROUND
		}
	}
}