//***************************************************************************************************
// Audio
//  Copyright (c) Rylogic Ltd 2017
//***************************************************************************************************

#include "audio/util/stdafx.h"
#include "pr/audio/midi/midi.h"
#include "pr/audio/waves/wave_bank.h"

namespace pr::audio
{
	// Create a wave bank for a midi instrument
	void CreateMidiInstrumentWaveBank(char const* bank_name, std::filesystem::path const& root_dir, std::filesystem::path const& xwb_filepath, std::filesystem::path const& xml_instrument_filepath)
	{
		if (std::filesystem::exists(root_dir))
			throw std::exception(FmtS("Failed to create wave bank: '%S' does not exist", root_dir.c_str()));

		// Temporary data structure for the found notes
		struct Note
		{
			struct Sample
			{
				std::wstring m_vel_range;
				std::filesystem::path m_filepath;
				Sample(std::wstring vel, std::filesystem::path const& filepath)
					:m_vel_range(vel)
					,m_filepath(filepath)
				{}
			};
			using Samples = std::vector<Sample>;

			std::wstring m_id;
			std::wstring m_name;
			Samples m_samples;
		};
		std::map<std::wstring, Note> map;

		// The pattern for extracting MIDI id, sample name, velocity range, filename
		// <midi_id>-<sample_name>\<velocity_range min-max>\<filename.wav>
		std::wregex pattern(LR"((\d+)-(.*)\\(\d+-\d+)\\(.*\.wav))");

		// Build the map of notes
		for (auto& entry : std::filesystem::recursive_directory_iterator(root_dir))
		{
			if (entry.is_directory())
				continue;
			
			std::wsmatch m;
			auto fpath = entry.path();
			auto rel_path = std::filesystem::relative(fpath, root_dir).wstring();
			if (!std::regex_match(rel_path, m, pattern))
				continue;

			auto& id = m[1];
			auto& name = m[2];
			auto& vel = m[3];

			auto& note = map[id];
			note.m_id = id;
			note.m_name = name;
			note.m_samples.push_back(Note::Sample(vel, fpath));
		}

		// Build a wave bank and XML description of the instrument
		WaveBankBuilder builder;
		xml::Node instr("instrument");
		for (auto& kv : map)
		{
			// Create the XML entry
			auto& note = instr.add(xml::Node("note"));
			note.add(xml::Node("id", kv.second.m_id));
			note.add(xml::Node("name", kv.second.m_name));
			auto& samples = note.add(xml::Node("samples"));
			for (auto& sam : kv.second.m_samples)
			{
				samples.add(xml::Node("file", sam.m_filepath));
				samples.add(xml::Node("vel", sam.m_vel_range));
				samples.add(xml::Node("wave_idx", std::to_wstring(builder.Count())));

				// Add the wave to the wave bank
				builder.Add(sam.m_filepath.c_str());
			}
		}

		// Write out the wave bank and instrument file
		xml::Save(xml_instrument_filepath, instr);
		builder.Write(bank_name, xwb_filepath);
		builder.WriteHeader(bank_name, std::filesystem::path(xwb_filepath).replace_extension(L".h"));
	}
}
