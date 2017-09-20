//***************************************************************************************************
// Audio
//  Copyright (c) Rylogic Ltd 2017
//***************************************************************************************************

#include "audio/util/stdafx.h"
#include "pr/audio/midi/midi.h"
#include "pr/audio/waves/wave_bank.h"

namespace pr
{
	namespace audio
	{
		// Create a wave bank for a midi instrument
		void CreateMidiInstrumentWaveBank(char const* bank_name, wchar_t const* root_dir, wchar_t const* xwb_filepath, wchar_t const* xml_instrument_filepath)
		{
			if (filesys::DirectoryExists(root_dir))
				throw std::exception(FmtS("Failed to create wave bank: '%S' does not exist", root_dir));

			// Temporary data structure for the found notes
			struct Note
			{
				struct Sample
				{
					std::wstring m_vel_range;
					std::wstring m_filepath;
					Sample(std::wstring vel, std::wstring filepath)
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
			filesys::EnumFiles(root_dir, L"*.wav", [&](filesys::FindFiles const& ff)
			{
				auto rel_path = filesys::GetRelativePath<std::wstring>(root_dir, ff.fullpath2());

				std::wsmatch m;
				if (!std::regex_match(rel_path, m, pattern))
					return true;

				auto& id    = m[1];
				auto& name  = m[2];
				auto& vel   = m[3];
				auto fpath = ff.fullpath();

				auto& note  = map[id];
				note.m_id   = id;
				note.m_name = name;
				note.m_samples.push_back(Note::Sample(vel, fpath));
				return true;
			});

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
			builder.WriteHeader(bank_name, filesys::ChangeExtn<std::wstring>(xwb_filepath, L".h").c_str());
		}
	}
}
