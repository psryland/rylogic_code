//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2025
//*********************************************
#include "pr/view3d-12/ldraw/ldraw_commands.h"
#include "pr/view3d-12/ldraw/ldraw_parsing.h"
#include "view3d-12/src/dll/context.h"
#include "view3d-12/src/dll/v3d_window.h"
#include "view3d-12/src/ldraw/sources/ldraw_sources.h"

namespace pr::rdr12::ldraw
{
	Command CommandHandler<ECommandId::Invalid>::Parse(IReader&)
	{
		return { ECommandId::Invalid, {} };
	}
	void CommandHandler<ECommandId::Invalid>::Execute(Command&, SourceBase&, Context&)
	{
	}
	
	Command CommandHandler<ECommandId::AddToScene>::Parse(IReader& reader)
	{
		auto scene_id = reader.Int<int>();
		return { ECommandId::AddToScene, std::span<int const>{&scene_id, 1} };
	}
	void CommandHandler<ECommandId::AddToScene>::Execute(Command& cmd, SourceBase& source, Context& context)
	{
		// Look for the window to add objects to
		auto scene_id = cmd.m_data.as<int>();
		if (scene_id < 0 || scene_id >= isize(context.m_windows))
			return;

		// Add all objects from 'source' to 'window'
		auto& window = *context.m_windows[scene_id];
		for (auto& obj : source.m_output.m_objects)
			window.Add(obj.get());
	}
	
	Command CommandHandler<ECommandId::CameraToWorld>::Parse(IReader&)
	{
		return { ECommandId::Invalid, {} };
	}
	void CommandHandler<ECommandId::CameraToWorld>::Execute(Command&, SourceBase&, Context&)
	{
	}

	Command CommandHandler<ECommandId::CameraPosition>::Parse(IReader&)
	{
		return { ECommandId::Invalid, {} };
	}
	void CommandHandler<ECommandId::CameraPosition>::Execute(Command&, SourceBase&, Context&)
	{
	}

	Command CommandHandler<ECommandId::ObjectToWorld>::Parse(IReader&)
	{
		return { ECommandId::Invalid, {} };
	}
	void CommandHandler<ECommandId::ObjectToWorld>::Execute(Command&, SourceBase&, Context&)
	{
	}

	Command CommandHandler<ECommandId::Render>::Parse(IReader& reader)
	{
		auto scene_id = reader.Int<int>();
		return { ECommandId::Render, std::span<int const>{&scene_id, 1} };
	}
	void CommandHandler<ECommandId::Render>::Execute(Command& cmd, SourceBase&, Context& context)
	{
		// Look for the window to render
		auto scene_id = cmd.m_data.as<int>();
		if (scene_id < 0 || scene_id >= isize(context.m_windows))
			return;

		// Render the window
		auto& window = *context.m_windows[scene_id];
		window.Render();
	}

	// Process an ldraw command
	void ExecuteCommands(SourceBase& source, Context& context)
	{
		for (auto& cmd : source.m_output.m_commands)
		{
			try
			{
				// Process the command
				switch (cmd.m_id)
				{
					#define PR_LDRAW_PARSE_IMPL(name, hash)\
					case ECommandId::name:\
					{\
						CommandHandler<ECommandId::name>::Execute(cmd, source, context);\
						break;\
					}
					#define PR_LDRAW_PARSE(x) x(PR_LDRAW_PARSE_IMPL)
					PR_LDRAW_PARSE(PR_ENUM_LDRAW_COMMANDS)
					default:
					{
						assert(false); // to trap them here
						std::runtime_error("Unsupported command");
					}
				}
			}
			catch (std::exception const& ex)
			{
				context.ReportError(std::format("Command Error: {}", ex.what()).c_str(), "", 0, 0);
			}
		}

		// All commands have been executed
		source.m_output.m_commands.resize(0);
	}
}
