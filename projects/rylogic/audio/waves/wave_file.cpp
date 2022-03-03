//***************************************************************************************************
// Audio
//  Copyright (c) Rylogic Ltd 2017
//***************************************************************************************************
#pragma once

#include "pr/audio/forward.h"
#include "pr/audio/waves/wave_file.h"

using namespace std::filesystem;

namespace pr::audio
{
	// 'wav' files types
	uint32_t const FOURCC_RIFF_TAG      = 'FFIR';
	uint32_t const FOURCC_FORMAT_TAG    = ' tmf';
	uint32_t const FOURCC_DATA_TAG      = 'atad';
	uint32_t const FOURCC_WAVE_FILE_TAG = 'EVAW';
	uint32_t const FOURCC_XWMA_FILE_TAG = 'AMWX';
	uint32_t const FOURCC_DLS_SAMPLE    = 'pmsw';
	uint32_t const FOURCC_MIDI_SAMPLE   = 'lpms';
	uint32_t const FOURCC_XWMA_DPDS     = 'sdpd';
	uint32_t const FOURCC_XMA_SEEK      = 'kees';

	#pragma region WAV structures
	#pragma pack(push,1)
	struct RIFFChunk
	{
		uint32_t tag;
		uint32_t size;
	};
	struct RIFFChunkHeader
	{
		uint32_t tag;
		uint32_t size;
		uint32_t riff;
	};
	struct DLSLoop
	{
		static const uint32_t LOOP_TYPE_FORWARD = 0x00000000;
		static const uint32_t LOOP_TYPE_RELEASE = 0x00000001;

		uint32_t size;
		uint32_t loopType;
		uint32_t loopStart;
		uint32_t loopLength;
	};
	struct RIFFDLSSample
	{
		static const uint32_t OPTIONS_NOTRUNCATION = 0x00000001;
		static const uint32_t OPTIONS_NOCOMPRESSION = 0x00000002;

		uint32_t    size;
		uint16_t    unityNote;
		int16_t     fineTune;
		int32_t     gain;
		uint32_t    options;
		uint32_t    loopCount;
	};
	struct MIDILoop
	{
		static const uint32_t LOOP_TYPE_FORWARD     = 0x00000000;
		static const uint32_t LOOP_TYPE_ALTERNATING = 0x00000001;
		static const uint32_t LOOP_TYPE_BACKWARD    = 0x00000002;

		uint32_t cuePointId;
		uint32_t type;
		uint32_t start;
		uint32_t end;
		uint32_t fraction;
		uint32_t playCount;
	};
	struct RIFFMIDISample
	{
		uint32_t        manufacturerId;
		uint32_t        productId;
		uint32_t        samplePeriod;
		uint32_t        unityNode;
		uint32_t        pitchFraction;
		uint32_t        SMPTEFormat;
		uint32_t        SMPTEOffset;
		uint32_t        loopCount;
		uint32_t        samplerData;
	};
	#pragma pack(pop)

	static_assert( sizeof(RIFFChunk)       ==  8, "structure size mismatch");
	static_assert( sizeof(RIFFChunkHeader) == 12, "structure size mismatch");
	static_assert( sizeof(DLSLoop)         == 16, "structure size mismatch");
	static_assert( sizeof(RIFFDLSSample)   == 20, "structure size mismatch");
	static_assert( sizeof(MIDILoop)        == 24, "structure size mismatch");
	static_assert( sizeof(RIFFMIDISample)  == 36, "structure size mismatch");
	#pragma endregion

	// Scan forward over 'data' searching for a chunk with a tag matching 'tag'
	RIFFChunk const* FindChunk(uint8_t const* data, size_t size_in_bytes, uint32_t tag)
	{
		if (data == nullptr)
			return nullptr;

		auto ptr = data;
		auto end = data + size_in_bytes;
		for (; ptr + sizeof(RIFFChunk) <= end;)
		{
			auto header = reinterpret_cast<const RIFFChunk*>(ptr);
			if (header->tag == tag)
				return header;

			auto offset = header->size + sizeof(RIFFChunk);
			ptr += offset;
		}

		return nullptr;
	}

	// Locate the wave format data within 'wav_data'
	void WaveFindFormatAndData(uint8_t const* wav_data, size_t wav_data_size, WAVEFORMATEX const*& wfx, uint8_t const*& data, uint32_t& data_size, bool& dpds, bool& seek)
	{
		assert("'wav_data' cannot be null" && wav_data != nullptr);
		auto wav_data_end = wav_data + wav_data_size;

		wfx = nullptr;
		data = nullptr;
		data_size = 0;
		dpds = false;
		seek = false;

		if (wav_data_size < sizeof(RIFFChunk)*2 + sizeof(uint32_t) + sizeof(WAVEFORMAT))
			throw std::runtime_error("WaveFindFormatAndData: Insufficient data.");

		// Locate RIFF 'WAVE'
		auto riff_chunk = FindChunk(wav_data, wav_data_size, FOURCC_RIFF_TAG);
		if (!riff_chunk || riff_chunk->size < 4)
			throw std::runtime_error("RIFF chunk not found");

		// Locate the RIFF header
		auto riff_header = reinterpret_cast<const RIFFChunkHeader*>(riff_chunk);
		if (riff_header->riff != FOURCC_WAVE_FILE_TAG && riff_header->riff != FOURCC_XWMA_FILE_TAG)
			throw std::runtime_error("RIFF chunk header not valid");

		// Check all of the RIFF chunk is there
		auto ptr = reinterpret_cast<const uint8_t*>(riff_header) + sizeof(RIFFChunkHeader);
		if (ptr + sizeof(RIFFChunk) > wav_data_end)
			throw std::runtime_error("RIFF chunk header incomplete");

		// Locate the format chunk
		auto fmt_chunk = FindChunk(ptr, riff_header->size, FOURCC_FORMAT_TAG);
		if (!fmt_chunk || fmt_chunk->size < sizeof(PCMWAVEFORMAT))
			throw std::runtime_error("Format chunk not found");

		// Check all of the format chunk is there
		ptr = reinterpret_cast<const uint8_t*>(fmt_chunk) + sizeof(RIFFChunk);
		if (ptr + fmt_chunk->size > wav_data_end)
			throw std::runtime_error("Format chunk header incomplete");

		// Validate WAVEFORMAT (focused on chunk size and format tag, not other data that XAUDIO2 will validate)
		auto wf = reinterpret_cast<WAVEFORMATEX const*>(ptr);
		switch ((EWaveFormat)wf->wFormatTag)
		{
		case EWaveFormat::PCM:
		case EWaveFormat::IEEE_FLOAT:
			{
				// Can be a PCMWAVEFORMAT (8 bytes) or WAVEFORMATEX (10 bytes)
				// We validated chunk as at least sizeof(PCMWAVEFORMAT) above
				break;
			}
		default:
			{
				if (fmt_chunk->size < sizeof(WAVEFORMATEX) ||
					fmt_chunk->size < sizeof(WAVEFORMATEX) + wf->cbSize)
					throw std::runtime_error("WAVEFORMATEX chunk is invalid");

				switch ((EWaveFormat)wf->wFormatTag)
				{
				default:
					{
						throw std::runtime_error(pr::FmtS("'wav_data' is not a supported type: %X", wf->wFormatTag));
					}
				case EWaveFormat::WMAUDIO2:
				case EWaveFormat::WMAUDIO3: // xWMA is supported by XAudio 2.7 and by XBox One
					{
						dpds = true;
						break;
					}
				case  EWaveFormat::XMA2: // XMA2 is supported by XBox One
					{
						auto const sizeof_WAVEFORMATEX = 18;
						auto const sizeof_XMA2WAVEFORMATEX = 52;
						if (fmt_chunk->size < sizeof_XMA2WAVEFORMATEX || wf->cbSize < sizeof_XMA2WAVEFORMATEX - sizeof_WAVEFORMATEX)
							throw std::runtime_error("'wav_data' of format 'EWaveFormat::XMA2' is incomplete");

						seek = true;
						break;
					}
				case EWaveFormat::ADPCM:
					{
						if (fmt_chunk->size < sizeof(WAVEFORMATEX) + MSADPCM_FORMAT_EXTRA_BYTES || wf->cbSize < MSADPCM_FORMAT_EXTRA_BYTES)
							throw std::runtime_error("'wav_data' of format 'EWaveFormat::ADPCM' is incomplete");

						break;
					}
				case EWaveFormat::EXTENSIBLE:
					{
						if (fmt_chunk->size < sizeof(WAVEFORMATEXTENSIBLE) || wf->cbSize < sizeof(WAVEFORMATEXTENSIBLE) - sizeof(WAVEFORMATEX))
							throw std::runtime_error("'wav_data' of format 'EWaveFormat::EXTENSIBLE' is incomplete");

						// WAVEFORMAT extensible
						static const GUID s_wfexBase = {0x00000000, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71};
						auto wfex = reinterpret_cast<WAVEFORMATEXTENSIBLE const*>(ptr);

						auto sub_format = reinterpret_cast<BYTE const*>(&wfex->SubFormat) + sizeof(DWORD);
						auto wfxe_base = reinterpret_cast<BYTE const*>(&s_wfexBase) + sizeof(DWORD);
						if (memcmp(sub_format, wfxe_base, sizeof(GUID) - sizeof(DWORD)) != 0)
							throw std::runtime_error("'wav_data' of format 'EWaveFormat::EXTENSIBLE' has an unsupported sub format");

						// Parse the sub format
						switch ((EWaveFormat)wfex->SubFormat.Data1)
						{
						default:
							throw std::runtime_error(pr::FmtS("'wav_data' of format 'EWaveFormat::EXTENSIBLE' has an unsupported sub format: %X", wfex->SubFormat.Data1));
						case EWaveFormat::PCM:
						case EWaveFormat::IEEE_FLOAT:
							break;
						// MS-ADPCM and XMA2 are not supported as WAVEFORMATEXTENSIBLE
						case EWaveFormat::WMAUDIO2:
						case EWaveFormat::WMAUDIO3:
							dpds = true;
							break;
						}

						break;
					}
				}
			}
		}

		// Locate 'data'
		ptr = reinterpret_cast<uint8_t const*>(riff_header) + sizeof(RIFFChunkHeader);
		if (ptr + sizeof(RIFFChunk) > wav_data_end)
			throw std::runtime_error("RIFF chunk is incomplete");

		// Locate the data chunk
		auto data_chunk = FindChunk(ptr, riff_chunk->size, FOURCC_DATA_TAG);
		if (!data_chunk || !data_chunk->size)
			throw std::runtime_error("Wave data chunk is invalid");

		// Check all the data is there
		ptr = reinterpret_cast<uint8_t const*>(data_chunk) + sizeof(RIFFChunk);
		if (ptr + data_chunk->size > wav_data_end)
			throw std::runtime_error("Wave data chunk is incomplete");

		// Result the results
		wfx = wf;
		data = ptr;
		data_size = data_chunk->size;
	}

	// Locate loop data within 'wav_data'. 'loop_start' and 'loop_length' will be 0 if not found
	void WaveFindLoopInfo(uint8_t const* wav_data, size_t wav_data_size, uint32_t& loop_start, uint32_t& loop_length)
	{
		assert("'wav_data' cannot be null" && wav_data != nullptr);
		auto wav_data_end = wav_data + wav_data_size;

		loop_start = 0;
		loop_length = 0;

		if (wav_data_size < sizeof(RIFFChunk) + sizeof(uint32_t))
			throw std::runtime_error("WaveFindFormatAndData: Insufficient data.");

		// Locate RIFF 'WAVE'
		auto riff_chunk = FindChunk(wav_data, wav_data_size, FOURCC_RIFF_TAG);
		if (!riff_chunk || riff_chunk->size < 4)
			throw std::runtime_error("RIFF chunk not found");

		// Locate the RIFF header
		auto riff_header = reinterpret_cast<const RIFFChunkHeader*>(riff_chunk);
		if (riff_header->riff == FOURCC_XWMA_FILE_TAG)
			return; // xWMA files do not contain loop information

		// Check this is a wave file
		if (riff_header->riff != FOURCC_WAVE_FILE_TAG)
			throw std::runtime_error("RIFF chunk does not have the wave file tag");

		// Check the RIFF chunk is complete
		auto ptr = reinterpret_cast<const uint8_t*>(riff_header) + sizeof(RIFFChunkHeader);
		if (ptr + sizeof(RIFFChunk) > wav_data_end)
			throw std::runtime_error("RIFF chunk is incomplete");

		// Locate 'wsmp' (DLS Chunk)
		auto dls_chunk = FindChunk(ptr, riff_chunk->size, FOURCC_DLS_SAMPLE);
		if (dls_chunk)
		{
			ptr = reinterpret_cast<const uint8_t*>(dls_chunk) + sizeof(RIFFChunk);
			if (ptr + dls_chunk->size > wav_data_end)
				throw std::runtime_error("DLS chunk is incomplete");

			// Ensure at least one sample
			if (dls_chunk->size >= sizeof(RIFFDLSSample))
			{
				auto dls_sample = reinterpret_cast<RIFFDLSSample const*>(ptr);
				if (dls_chunk->size >= dls_sample->size + dls_sample->loopCount * sizeof(DLSLoop))
				{
					auto loops = reinterpret_cast<DLSLoop const*>(ptr + dls_sample->size);
					for (uint32_t j = 0; j < dls_sample->loopCount; ++j)
					{
						if (loops[j].loopType == DLSLoop::LOOP_TYPE_FORWARD || loops[j].loopType == DLSLoop::LOOP_TYPE_RELEASE)
						{
							// Return 'forward' loop
							loop_start = loops[j].loopStart;
							loop_length = loops[j].loopLength;
							return;
						}
					}
				}
			}
		}

		// Locate 'smpl' (Sample Chunk)
		auto midi_chunk = FindChunk(ptr, riff_chunk->size, FOURCC_MIDI_SAMPLE);
		if (midi_chunk)
		{
			ptr = reinterpret_cast<uint8_t const*>(midi_chunk) + sizeof(RIFFChunk);
			if (ptr + midi_chunk->size > wav_data_end)
				throw std::runtime_error("MIDI chunk is incomplete");

			if (midi_chunk->size >= sizeof(RIFFMIDISample))
			{
				auto midi_sample = reinterpret_cast<RIFFMIDISample const*>(ptr);
				if (midi_chunk->size >= sizeof(RIFFMIDISample) + midi_sample->loopCount * sizeof(MIDILoop))
				{
					auto loops = reinterpret_cast<MIDILoop const*>(ptr + sizeof(RIFFMIDISample));
					for (uint32_t j = 0; j < midi_sample->loopCount; ++j)
					{
						if (loops[j].type == MIDILoop::LOOP_TYPE_FORWARD)
						{
							// Return 'forward' loop
							loop_start = loops[j].start;
							loop_length = loops[j].end + loops[j].start + 1;
							return;
						}
					}
				}
			}
		}
	}

	// Locate the table
	void WaveFindTable(uint8_t const* wav_data, size_t wav_data_size, uint32_t tag, uint32_t const*& data, uint32_t& data_count)
	{
		assert("'wav_data' cannot be null" && wav_data != nullptr);
		auto wav_data_end = wav_data + wav_data_size;

		data = nullptr;
		data_count = 0;

		if (wav_data_size < sizeof(RIFFChunk) + sizeof(uint32_t))
			throw std::runtime_error("WaveFindTable: Insufficient data.");

		// Locate RIFF 'WAVE'
		auto riff_chunk = FindChunk(wav_data, wav_data_size, FOURCC_RIFF_TAG);
		if (!riff_chunk || riff_chunk->size < 4)
			throw std::runtime_error("RIFF chunk not found");

		// Locate the RIFF header
		auto riff_header = reinterpret_cast<const RIFFChunkHeader*>(riff_chunk);
		if (riff_header->riff != FOURCC_WAVE_FILE_TAG && riff_header->riff != FOURCC_XWMA_FILE_TAG)
			throw std::runtime_error("RIFF chunk header not valid");

		// Check all of the RIFF chunk is there
		auto ptr = reinterpret_cast<const uint8_t*>(riff_header) + sizeof(RIFFChunkHeader);
		if (ptr + sizeof(RIFFChunk) > wav_data_end)
			throw std::runtime_error("RIFF chunk header incomplete");

		// Locate tag
		auto table_chunk = FindChunk(ptr, riff_chunk->size, tag);
		if (table_chunk)
		{
			ptr = reinterpret_cast<uint8_t const*>(table_chunk) + sizeof(RIFFChunk);
			if (ptr + table_chunk->size > wav_data_end)
				throw std::runtime_error("Table chunk is incomplete");

			if ((table_chunk->size % sizeof(uint32_t)) != 0)
				throw std::runtime_error("Table chunk is invalid (not a multiple of 4 bytes)");

			data = reinterpret_cast<uint32_t const*>(ptr);
			data_count = table_chunk->size / 4;
		}
	}

	// Load a WAV file from a filepath into a memory buffer
	void LoadAudioFromFile(std::filesystem::path const& filepath, std::unique_ptr<uint8_t[]>& wav_data, DWORD& bytes_read)
	{
		if (!exists(filepath))
			throw std::runtime_error(FmtS("Audio file '%S' doesn't exist", filepath.c_str()));

		// Get the file size
		auto file_size = std::filesystem::file_size(filepath);

		// Open the file
		auto file = win32::FileOpen(filepath, GENERIC_READ, FILE_SHARE_READ, OPEN_EXISTING);
		if (file == INVALID_HANDLE_VALUE)
			pr::Throw(HRESULT_FROM_WIN32(GetLastError()));

		// File is too big for 32-bit allocation, so reject read
		if (file_size > 0xFFFFFFFF)
			throw std::runtime_error("Audio file is too big for 32-bit allocation");

		// Need at least enough data to have a valid minimal WAV file
		if (file_size < sizeof(RIFFChunk) * 2 + sizeof(DWORD) + sizeof(WAVEFORMAT))
			throw std::runtime_error("LoadAudioFromFile: Insufficient data.");

		// Create enough space for the file data
		wav_data.reset(new (std::nothrow) uint8_t[static_cast<size_t>(file_size)]);
		if (!wav_data)
			throw std::runtime_error(pr::FmtS("LoadAudioFromFile: Allocation of %u bytes failed", file_size));

		// Read the data into the buffer
		if (!ReadFile(file, wav_data.get(), s_cast<DWORD,true>(file_size), &bytes_read, nullptr))
			pr::Throw(HRESULT_FROM_WIN32(GetLastError()));
		if (bytes_read < file_size)
			throw std::runtime_error(pr::FmtS("LoadAudioFromFile: Partial read of wave file. Expected %u bytes, read %u bytes", file_size, bytes_read));
	}

	// Load and parse a wave file from data in memory
	void LoadWAVAudioInMemory(uint8_t const* wav_data, size_t wav_data_size, WAVEFORMATEX const*& wfx, uint8_t const*& audio_start, uint32_t& audio_bytes)
	{
		if (wav_data == nullptr)
			throw std::runtime_error("'wav_data' cannot be null");

		wfx = nullptr;
		audio_start = nullptr;
		audio_bytes = 0;

		// Need at least enough data to have a valid minimal WAV file
		if (wav_data_size < sizeof(RIFFChunk) * 2 + sizeof(DWORD) + sizeof(WAVEFORMAT))
			throw std::runtime_error("LoadWAVAudioInMemory: Insufficient data.");

		// Locate pointers to data in the 'wav' file
		bool dpds, seek;
		WaveFindFormatAndData(wav_data, wav_data_size, wfx, audio_start, audio_bytes, dpds, seek);
		if (dpds || seek)
			throw std::runtime_error("dpds || seek");
	}
	void LoadWAVAudioInMemory(uint8_t const* wav_data, size_t wav_data_size, WavData& result)
	{
		if (wav_data == nullptr)
			throw std::runtime_error("'wav_data' cannot be null");

		result = WavData{};

		// Need at least enough data to have a valid minimal WAV file
		if (wav_data_size < sizeof(RIFFChunk) * 2 + sizeof(DWORD) + sizeof(WAVEFORMAT))
			throw std::runtime_error("LoadWAVAudioInMemory: Insufficient data.");

		bool dpds, seek;
		WaveFindFormatAndData(wav_data, wav_data_size, result.wfx, result.audio_start, result.audio_bytes, dpds, seek);
		WaveFindLoopInfo(wav_data, wav_data_size, result.loop_start, result.loop_length);

		if (dpds)
			WaveFindTable(wav_data, wav_data_size, FOURCC_XWMA_DPDS, result.seek, result.seek_count);
		if (seek)
			WaveFindTable(wav_data, wav_data_size, FOURCC_XMA_SEEK, result.seek, result.seek_count);
	}

	// Load and parse a wave file from a file
	void LoadWAVAudioFromFile(std::filesystem::path const& filepath, std::unique_ptr<uint8_t[]>& wav_data, WAVEFORMATEX const*& wfx, uint8_t const*& audio_start, uint32_t& audio_bytes)
	{
		wfx = nullptr;
		audio_start = nullptr;
		audio_bytes = 0;

		DWORD bytes_read = 0;
		LoadAudioFromFile(filepath, wav_data, bytes_read);

		bool dpds, seek;
		WaveFindFormatAndData(wav_data.get(), bytes_read, wfx, audio_start, audio_bytes, dpds, seek);
		if (dpds || seek)
			throw std::runtime_error("dpds || seek");
	}
	void LoadWAVAudioFromFile(std::filesystem::path const& filepath, std::unique_ptr<uint8_t[]>& wav_data, WavData& result)
	{
		result = WavData{};

		bool dpds, seek;
		DWORD bytes_read = 0;
		LoadAudioFromFile(filepath, wav_data, bytes_read);
		WaveFindFormatAndData(wav_data.get(), bytes_read, result.wfx, result.audio_start, result.audio_bytes, dpds, seek);
		WaveFindLoopInfo(wav_data.get(), bytes_read, result.loop_start, result.loop_length);

		if (dpds)
			WaveFindTable(wav_data.get(), bytes_read, FOURCC_XWMA_DPDS, result.seek, result.seek_count);
		if (seek)
			WaveFindTable(wav_data.get(), bytes_read, FOURCC_XMA_SEEK, result.seek, result.seek_count);
	}
}