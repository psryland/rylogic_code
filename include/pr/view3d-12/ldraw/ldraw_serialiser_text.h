//********************************
// Ldraw Script Text Serialiser
//  Copyright (c) Rylogic Ltd 2025
//********************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/ldraw/ldraw.h"
#include "pr/view3d-12/ldraw/ldraw_parsing.h"

namespace pr::rdr12::ldraw
{
	struct TextReader : IReader
	{
		std::unique_ptr<IReader> m_impl;

		TextReader(std::istream& src, EEncoding enc = EEncoding::utf8, std::filesystem::path src_filepath = {}, ReportErrorCB report_error_cb = nullptr, ParseProgressCB progress_cb = nullptr, IPathResolver const& resolver = PathResolver::Instance());
		TextReader(TextReader&&) = delete;
		TextReader(TextReader const&) = delete;
		TextReader& operator=(TextReader&&) = delete;
		TextReader& operator=(TextReader const&) = delete;

		// Return the current location in the source
		virtual Location const& Loc() const override
		{
			return m_impl->Loc();
		}

		// Move into a nested section
		virtual void PushSection() override
		{
			m_impl->PushSection();
		}

		// Leave the current nested section
		virtual void PopSection() override
		{
			m_impl->PopSection();
		}

		// Get the next keyword within the current section.
		// Returns false if at the end of the section
		virtual bool NextKeyword(ldraw::EKeyword& kw) override
		{
			return m_impl->NextKeyword(kw);
		}

		// True when the current position has reached the end of the current section
		virtual bool IsSectionEnd() override
		{
			return m_impl->IsSectionEnd();
		}

		// Read a utf8 string from the current section.
		// If 'has_length' is false, assume the whole section is the string.
		// If 'has_length' is true, assume the string is prefixed by its length.
		virtual string32 StringImpl(bool has_length = false) override
		{
			return m_impl->StringImpl(has_length);
		}

		// Read an integral value from the current section
		virtual int64_t IntImpl(int byte_count, int radix) override
		{
			return m_impl->IntImpl(byte_count, radix);
		}

		// Read a floating point value from the current section
		virtual double RealImpl(int byte_count) override
		{
			return m_impl->RealImpl(byte_count);
		}
	};
}
