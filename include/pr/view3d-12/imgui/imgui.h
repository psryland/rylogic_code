//********************************
// ImGui integration for view3d-12
//  Copyright (c) Rylogic Ltd 2025
//********************************
// Notes:
//  - This provides a DLL-based integration of Dear ImGui with the view3d-12 renderer.
//  - All imgui types are hidden within the DLL. The client sees only a C-style API.
//  - To avoid making this a build dependency, this header dynamically loads 'imgui.dll' as needed.
//  - The DLL manages its own imgui context, descriptor heap, and pipeline state objects.
#pragma once
#include <cstdint>
#include <string>
#include "pr/common/assert.h"
#include "pr/win32/win32.h"

// Forward declarations for D3D12 types (no d3d12.h dependency here)
struct ID3D12Device;
struct ID3D12GraphicsCommandList;

namespace pr::rdr12::imgui
{
	// Opaque DLL context handle. Defined within the DLL.
	struct Context;

	// Initialisation parameters passed to the DLL
	struct InitArgs
	{
		ID3D12Device* m_device;          // D3D12 device
		HWND m_hwnd;                     // Window handle for input
		DXGI_FORMAT m_rtv_format;        // Render target format
		int m_num_frames_in_flight;      // Number of buffered frames (typically 2-3)
	};

	// Error handling callback
	struct ErrorHandler
	{
		using FuncCB = void(*)(void*, char const* msg, size_t len);

		void* m_ctx;
		FuncCB m_cb;

		void operator()(std::string_view message) const
		{
			if (m_cb) m_cb(m_ctx, message.data(), message.size());
			else throw std::runtime_error(std::string(message));
		}
	};

	// Dynamically loaded ImGui DLL
	class ImGuiDll
	{
		friend struct ImGuiUI;
		HMODULE m_module;

		#define PR_IMGUI_API(x)\
		x(, Initialise        , Context* (__stdcall*)(InitArgs const& args, ErrorHandler error_cb))\
		x(, Shutdown           , void (__stdcall*)(Context* ctx))\
		x(, NewFrame           , void (__stdcall*)(Context& ctx))\
		x(, Render             , void (__stdcall*)(Context& ctx, ID3D12GraphicsCommandList* cmd_list))\
		x(, WndProc            , bool (__stdcall*)(Context& ctx, HWND hwnd, UINT msg, WPARAM wparam, LPARAM lparam))\
		x(, Text               , void (__stdcall*)(Context& ctx, char const* text))\
		x(, BeginWindow        , bool (__stdcall*)(Context& ctx, char const* name, bool* p_open, int flags))\
		x(, EndWindow          , void (__stdcall*)(Context& ctx))\
		x(, SetNextWindowPos   , void (__stdcall*)(Context& ctx, float x, float y, int cond))\
		x(, SetNextWindowSize  , void (__stdcall*)(Context& ctx, float w, float h, int cond))\
		x(, SetNextWindowBgAlpha , void (__stdcall*)(Context& ctx, float alpha))\
		x(, Checkbox           , bool (__stdcall*)(Context& ctx, char const* label, bool* v))\
		x(, SliderFloat        , bool (__stdcall*)(Context& ctx, char const* label, float* v, float v_min, float v_max))\
		x(, Button             , bool (__stdcall*)(Context& ctx, char const* label))\
		x(, SameLine           , void (__stdcall*)(Context& ctx, float offset_from_start_x, float spacing))\
		x(, Separator          , void (__stdcall*)(Context& ctx))
		#define PR_IMGUI_FUNCTION_MEMBERS(prefix, name, function_type) using prefix##name##Fn = function_type; prefix##name##Fn prefix##name = {};
		PR_IMGUI_API(PR_IMGUI_FUNCTION_MEMBERS)
		#undef PR_IMGUI_FUNCTION_MEMBERS

		ImGuiDll()
			: m_module(win32::LoadDll<struct ImGuiDllTag>("imgui.dll"))
		{
			#pragma warning(push)
			#pragma warning(disable: 4191)
			#define PR_IMGUI_GET_PROC_ADDRESS(prefix, name, function_type) prefix##name = reinterpret_cast<prefix##name##Fn>(GetProcAddress(m_module, "ImGui_" #prefix #name));
			PR_IMGUI_API(PR_IMGUI_GET_PROC_ADDRESS)
			#undef PR_IMGUI_GET_PROC_ADDRESS
			#pragma warning(pop)
		}

		static ImGuiDll& get() { static ImGuiDll s_this; return s_this; }
	};

	// RAII wrapper for the imgui DLL context. This is the client-side API.
	struct ImGuiUI
	{
		Context* m_ctx;

		ImGuiUI()
			: m_ctx()
		{}
		ImGuiUI(InitArgs const& args, ErrorHandler error_cb = {})
			: m_ctx(ImGuiDll::get().Initialise(args, error_cb))
		{}
		ImGuiUI(ImGuiUI&& rhs) noexcept
			: m_ctx()
		{
			std::swap(m_ctx, rhs.m_ctx);
		}
		ImGuiUI(ImGuiUI const&) = delete;
		ImGuiUI& operator=(ImGuiUI&& rhs) noexcept
		{
			if (this != &rhs) std::swap(m_ctx, rhs.m_ctx);
			return *this;
		}
		ImGuiUI& operator=(ImGuiUI const&) = delete;
		~ImGuiUI()
		{
			if (m_ctx)
				ImGuiDll::get().Shutdown(m_ctx);
		}

		explicit operator bool() const { return m_ctx != nullptr; }

		// Start a new imgui frame. Call before any imgui widget functions.
		void NewFrame()
		{
			PR_ASSERT(PR_DBG, m_ctx != nullptr, "ImGuiUI not initialised");
			ImGuiDll::get().NewFrame(*m_ctx);
		}

		// Render the imgui draw data into the command list
		void Render(ID3D12GraphicsCommandList* cmd_list)
		{
			PR_ASSERT(PR_DBG, m_ctx != nullptr, "ImGuiUI not initialised");
			ImGuiDll::get().Render(*m_ctx, cmd_list);
		}

		// Forward a Win32 message to imgui. Returns true if imgui consumed the message.
		bool WndProc(HWND hwnd, UINT msg, WPARAM wparam, LPARAM lparam)
		{
			if (!m_ctx) return false;
			return ImGuiDll::get().WndProc(*m_ctx, hwnd, msg, wparam, lparam);
		}

		// Widget helpers
		void Text(char const* text) { ImGuiDll::get().Text(*m_ctx, text); }
		bool BeginWindow(char const* name, bool* p_open = nullptr, int flags = 0) { return ImGuiDll::get().BeginWindow(*m_ctx, name, p_open, flags); }
		void EndWindow() { ImGuiDll::get().EndWindow(*m_ctx); }
		void SetNextWindowPos(float x, float y, int cond = 0) { ImGuiDll::get().SetNextWindowPos(*m_ctx, x, y, cond); }
		void SetNextWindowSize(float w, float h, int cond = 0) { ImGuiDll::get().SetNextWindowSize(*m_ctx, w, h, cond); }
		void SetNextWindowBgAlpha(float alpha) { ImGuiDll::get().SetNextWindowBgAlpha(*m_ctx, alpha); }
		bool Checkbox(char const* label, bool* v) { return ImGuiDll::get().Checkbox(*m_ctx, label, v); }
		bool SliderFloat(char const* label, float* v, float v_min, float v_max) { return ImGuiDll::get().SliderFloat(*m_ctx, label, v, v_min, v_max); }
		bool Button(char const* label) { return ImGuiDll::get().Button(*m_ctx, label); }
		void SameLine(float offset_from_start_x = 0.0f, float spacing = -1.0f) { ImGuiDll::get().SameLine(*m_ctx, offset_from_start_x, spacing); }
		void Separator() { ImGuiDll::get().Separator(*m_ctx); }
	};
}
