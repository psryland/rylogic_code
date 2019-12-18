//***************************************************************************************************
// Audio
//  Copyright (c) Rylogic Ltd 2017
//***************************************************************************************************
#pragma once

#include "pr/audio/forward.h"

namespace pr::audio
{
	class WaveBankReader
	{
		struct Impl;
		std::unique_ptr<Impl> pImpl;

	public:

		struct Metadata
		{
			uint32_t    duration;
			uint32_t    loopStart;
			uint32_t    loopLength;
			uint32_t    offsetBytes;
			uint32_t    lengthBytes;
		};

		WaveBankReader();
		WaveBankReader(WaveBankReader const&) = delete;
		WaveBankReader& operator= (WaveBankReader const&) = delete;

		// Open a wave bank
		void Open(std::filesystem::path const& szFileName);

		// True if the non-streaming wave bank is completed loaded into memory
		bool IsPrepared();

		// Block until the non streaming bank is fully loaded into memory
		void WaitOnPrepare();

		// True if the wave bank has names for each wave
		bool HasNames() const;

		// Look up the index of a wave with the given name (-1 if not found)
		int Find(const char* name) const;

		// True if this bank supports streaming
		bool IsStreamingBank() const;

		// The name of this wave bank
		const char* BankName() const;

		// The number of waves in this wave bank
		int Count() const;

		// The size of the audio data
		uint32_t BankAudioSize() const;

		// Get the format of a wave in the bank
		void GetFormat(int index, WaveFormatsU& format) const;

		// Get a pointer to the wave data for a wave in the bank
		void GetWaveData(int index, uint8_t const*& data, uint32_t& dataSize) const;

		// Get a pointer to the seek table in the bank
		void GetSeekTable(int index, uint32_t const*& data, uint32_t& data_count, uint32_t& tag) const;

		// Get the meta data for a wave in the bank
		void GetMetadata(int index, Metadata& metadata) const;

		// Get the file handle used for overlapped reads (streaming wave bank only)
		HANDLE GetAsyncHandle() const;
	};

	class WaveBankBuilder
	{
		struct Impl;
		std::shared_ptr<Impl> pImpl;

	public:

		enum class EOptions
		{
			None          = 0,
			Streaming     = 1 << 0,
			Compact       = 1 << 1,
			FriendlyNames = 1 << 2,
			Overwrite     = 1 << 3,
			_bitwise_operators_allowed,
		};

		WaveBankBuilder();

		// Reset the builder
		void Clear();

		// Add a wave file to the wave bank
		void Add(std::filesystem::path const& filepath);

		// The number of waves added so far
		int Count() const;

		// Write the a wave bank to a stream
		void Write(char const* bank_name, std::ostream& xwb, EOptions opts = EOptions::None) const;
		void Write(char const* bank_name, std::filesystem::path const& xwb_filepath, EOptions opts = EOptions::None) const;

		// Write a C header for the wave bank
		void WriteHeader(char const* bank_name, std::ostream& hdr) const;
		void WriteHeader(char const* bank_name, std::filesystem::path const& header_filepath, EOptions opts = EOptions::None) const;
	};
}
