//***************************************************************************************************
// Audio
//  Copyright (c) Rylogic Ltd 2017
//***************************************************************************************************
#pragma once

#include "pr/audio/audio/forward.h"

namespace pr
{
	namespace audio
	{
		// WAV file header
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
}