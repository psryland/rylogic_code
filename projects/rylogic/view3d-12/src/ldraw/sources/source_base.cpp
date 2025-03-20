//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "view3d-12/src/ldraw/sources/source_base.h"

namespace pr::rdr12::ldraw
{
	SourceBase::SourceBase(Guid const* context_id)
		: m_output()
		, m_context_id(context_id ? *context_id : GenerateGUID())
		, m_filepaths()
		, m_errors()
		, ParsingProgress()
	{}

	// Construct a new instance of the source (if possible)
	std::unique_ptr<SourceBase> SourceBase::Clone()
	{
		return nullptr;
	}

	// Parse the contents of the script
	void SourceBase::Reload(Renderer& rdr)
	{
		try
		{
			m_errors.resize(0);
			m_filepaths.resize(0);
			m_output = ReadSource(rdr);
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
		return m_output;
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

