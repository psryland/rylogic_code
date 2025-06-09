//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "view3d-12/src/ldraw/sources/source_base.h"

namespace pr::rdr12::ldraw
{
	SourceBase::SourceBase(Guid const* context_id)
		: m_name()
		, m_output()
		, m_context_id(context_id ? *context_id : GenerateGUID())
		, m_filepaths()
		, m_errors()
		, ParsingProgress()
	{
		m_name = To<string32>(m_context_id);
	}

	// Construct a new instance of the source (if possible)
	std::shared_ptr<SourceBase> SourceBase::Clone()
	{
		return nullptr;
	}

	// Parse the contents of the script
	void SourceBase::Load(Renderer& rdr, EDataChangedReason trigger, AddCompleteCB add_complete_cb)
	{
		try
		{
			m_output = ReadSource(rdr);

			// Only notify if there are attached handlers. This allows stack allocated Sources
			// to be used (that won't work with shared_from_this()) if they don't attach to 'Notify'.
			if (Notify)
				Notify(shared_from_this(), { ENotifyReason::LoadComplete, trigger, add_complete_cb });
		}
		catch (std::exception const& ex)
		{
			ParseErrorEventArgs err{ ex.what(), ldraw::EParseError::UnknownError, {} };
			m_errors.push_back(err);
		}
	}

	// Regenerate the output from the source
	ParseResult SourceBase::ReadSource(Renderer&)
	{
		return std::move(m_output);
	}
	
	// Create a stable Guid from a filepath
	Guid ContextIdFromFilepath(std::filesystem::path const& filepath)
	{
		static Guid const LdrawSourceFileNS = { 0xA9C66A7D, 0xD1F3, 0x4CFA, 0x84, 0xE0, 0xCF, 0x99, 0x12, 0xB3, 0x18, 0x9D };
		return GenerateGUID(LdrawSourceFileNS, filepath.lexically_normal().string().c_str());
	}

	// Callbacks for parsing
	void __stdcall SourceBase::OnReportError(void* ctx, EParseError err, Location const& loc, std::string_view msg)
	{
		auto this_ = static_cast<SourceBase*>(ctx);
		this_->m_errors.push_back(ParseErrorEventArgs{ msg, err, loc });
	}
	bool __stdcall SourceBase::OnProgress(void* ctx, Guid const& context_id, ParseResult const& out, Location const& loc, bool complete)
	{
		auto this_ = static_cast<SourceBase*>(ctx);
		ParsingProgressEventArgs args{ context_id, out, loc, complete };
		this_->ParsingProgress(*this_, args);
		return !args.m_cancel;
	}
}

