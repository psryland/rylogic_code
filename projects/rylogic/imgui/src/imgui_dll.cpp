//********************************
// ImGui DLL for view3d-12
//  Copyright (c) Rylogic Ltd 2025
//********************************
// Implements the C-style API for the imgui DLL.
// All imgui types are contained within this DLL.
// This file does NOT include the client header (pr/view3d-12/imgui/imgui.h)
// to avoid type name collisions between imgui's ImGuiContext and ours.

// imgui implementation (unity build)
#include "imgui.cpp"
#include "imgui_draw.cpp"
#include "imgui_tables.cpp"
#include "imgui_widgets.cpp"
#include "imgui_demo.cpp"

// imgui platform/renderer backends
#include "imgui_impl_win32.cpp"
#include "imgui_impl_dx12.cpp"

// Standard library
#include <d3d12.h>
#include <dxgi1_4.h>
#include <mutex>
#include <vector>
#include <memory>
#include <string>
#include <string_view>
#include <stdexcept>

#pragma comment(lib, "d3d12.lib")
#pragma comment(lib, "dxgi.lib")

// Forward declaration from imgui_impl_win32.cpp
extern IMGUI_IMPL_API LRESULT ImGui_ImplWin32_WndProcHandler(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam);

// Mirror the types from the client header so the ABI matches.
// These must be layout-compatible with the types in pr/view3d-12/imgui/imgui.h.
namespace dll
{
	struct InitArgs
	{
		ID3D12Device* m_device;
		HWND m_hwnd;
		DXGI_FORMAT m_rtv_format;
		int m_num_frames_in_flight;
	};

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

	// The internal context holding all imgui state. Opaque to the client.
	struct Context
	{
		ImGuiContext* m_imgui_ctx;
		ID3D12Device* m_device;
		ID3D12DescriptorHeap* m_srv_heap;
		ErrorHandler m_error_cb;

		Context(InitArgs const& args, ErrorHandler error_cb)
			: m_imgui_ctx(nullptr)
			, m_device(args.m_device)
			, m_srv_heap(nullptr)
			, m_error_cb(error_cb)
		{
			try
			{
				// Create a descriptor heap for imgui's font texture SRV
				D3D12_DESCRIPTOR_HEAP_DESC desc = {};
				desc.Type = D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV;
				desc.NumDescriptors = 1;
				desc.Flags = D3D12_DESCRIPTOR_HEAP_FLAG_SHADER_VISIBLE;
				auto hr = m_device->CreateDescriptorHeap(&desc, IID_PPV_ARGS(&m_srv_heap));
				if (FAILED(hr))
					throw std::runtime_error("Failed to create imgui descriptor heap");

				// Create imgui context
				m_imgui_ctx = ImGui::CreateContext();
				ImGui::SetCurrentContext(m_imgui_ctx);

				auto& io = ImGui::GetIO();
				io.ConfigFlags |= ImGuiConfigFlags_NavEnableKeyboard;

				// Initialise platform/renderer backends
				ImGui_ImplWin32_Init(args.m_hwnd);
				ImGui_ImplDX12_Init(
					m_device,
					args.m_num_frames_in_flight,
					args.m_rtv_format,
					m_srv_heap,
					m_srv_heap->GetCPUDescriptorHandleForHeapStart(),
					m_srv_heap->GetGPUDescriptorHandleForHeapStart()
				);
			}
			catch (std::exception const& ex)
			{
				Cleanup();
				if (m_error_cb.m_cb)
					m_error_cb(ex.what());
				else
					throw;
			}
		}
		~Context()
		{
			Cleanup();
		}

		void Cleanup()
		{
			if (m_imgui_ctx)
			{
				ImGui::SetCurrentContext(m_imgui_ctx);
				ImGui_ImplDX12_Shutdown();
				ImGui_ImplWin32_Shutdown();
				ImGui::DestroyContext(m_imgui_ctx);
				m_imgui_ctx = nullptr;
			}
			if (m_srv_heap)
			{
				m_srv_heap->Release();
				m_srv_heap = nullptr;
			}
		}
	};
}

// DLL global state
static std::mutex g_mutex;
static std::vector<std::unique_ptr<dll::Context>> g_contexts;
static HINSTANCE g_instance;

extern "C"
{
	BOOL APIENTRY DllMain(HINSTANCE hInstance, DWORD ul_reason_for_call, LPVOID)
	{
		(void)ul_reason_for_call;
		g_instance = hInstance;
		return TRUE;
	}

	using namespace dll;

	__declspec(dllexport) Context* __stdcall ImGui_Initialise(InitArgs const& args, ErrorHandler error_cb)
	{
		std::lock_guard<std::mutex> lock(g_mutex);
		g_contexts.push_back(std::make_unique<Context>(args, error_cb));
		return g_contexts.back().get();
	}

	__declspec(dllexport) void __stdcall ImGui_Shutdown(Context* ctx)
	{
		std::lock_guard<std::mutex> lock(g_mutex);
		auto it = std::remove_if(g_contexts.begin(), g_contexts.end(), [ctx](auto const& p) { return p.get() == ctx; });
		g_contexts.erase(it, g_contexts.end());
	}

	__declspec(dllexport) void __stdcall ImGui_NewFrame(Context& ctx)
	{
		ImGui::SetCurrentContext(ctx.m_imgui_ctx);
		ImGui_ImplDX12_NewFrame();
		ImGui_ImplWin32_NewFrame();
		ImGui::NewFrame();
	}

	__declspec(dllexport) void __stdcall ImGui_Render(Context& ctx, ID3D12GraphicsCommandList* cmd_list)
	{
		ImGui::SetCurrentContext(ctx.m_imgui_ctx);
		ImGui::Render();

		// Set the descriptor heap for imgui's font texture
		ID3D12DescriptorHeap* heaps[] = { ctx.m_srv_heap };
		cmd_list->SetDescriptorHeaps(1, heaps);

		ImGui_ImplDX12_RenderDrawData(ImGui::GetDrawData(), cmd_list);
	}

	__declspec(dllexport) bool __stdcall ImGui_WndProc(Context& ctx, HWND hwnd, UINT msg, WPARAM wparam, LPARAM lparam)
	{
		ImGui::SetCurrentContext(ctx.m_imgui_ctx);
		auto result = ImGui_ImplWin32_WndProcHandler(hwnd, msg, wparam, lparam);
		return result != 0;
	}

	__declspec(dllexport) void __stdcall ImGui_Text(Context& ctx, char const* text)
	{
		ImGui::SetCurrentContext(ctx.m_imgui_ctx);
		ImGui::TextUnformatted(text);
	}

	__declspec(dllexport) bool __stdcall ImGui_BeginWindow(Context& ctx, char const* name, bool* p_open, int flags)
	{
		ImGui::SetCurrentContext(ctx.m_imgui_ctx);
		return ImGui::Begin(name, p_open, static_cast<ImGuiWindowFlags_>(flags));
	}

	__declspec(dllexport) void __stdcall ImGui_EndWindow(Context& ctx)
	{
		ImGui::SetCurrentContext(ctx.m_imgui_ctx);
		ImGui::End();
	}

	__declspec(dllexport) void __stdcall ImGui_SetNextWindowPos(Context& ctx, float x, float y, int cond)
	{
		ImGui::SetCurrentContext(ctx.m_imgui_ctx);
		ImGui::SetNextWindowPos(ImVec2(x, y), static_cast<ImGuiCond>(cond));
	}

	__declspec(dllexport) void __stdcall ImGui_SetNextWindowSize(Context& ctx, float w, float h, int cond)
	{
		ImGui::SetCurrentContext(ctx.m_imgui_ctx);
		ImGui::SetNextWindowSize(ImVec2(w, h), static_cast<ImGuiCond>(cond));
	}

	__declspec(dllexport) void __stdcall ImGui_SetNextWindowBgAlpha(Context& ctx, float alpha)
	{
		ImGui::SetCurrentContext(ctx.m_imgui_ctx);
		ImGui::SetNextWindowBgAlpha(alpha);
	}

	__declspec(dllexport) bool __stdcall ImGui_Checkbox(Context& ctx, char const* label, bool* v)
	{
		ImGui::SetCurrentContext(ctx.m_imgui_ctx);
		return ImGui::Checkbox(label, v);
	}

	__declspec(dllexport) bool __stdcall ImGui_SliderFloat(Context& ctx, char const* label, float* v, float v_min, float v_max)
	{
		ImGui::SetCurrentContext(ctx.m_imgui_ctx);
		return ImGui::SliderFloat(label, v, v_min, v_max);
	}

	__declspec(dllexport) bool __stdcall ImGui_Button(Context& ctx, char const* label)
	{
		ImGui::SetCurrentContext(ctx.m_imgui_ctx);
		return ImGui::Button(label);
	}

	__declspec(dllexport) void __stdcall ImGui_SameLine(Context& ctx, float offset_from_start_x, float spacing)
	{
		ImGui::SetCurrentContext(ctx.m_imgui_ctx);
		ImGui::SameLine(offset_from_start_x, spacing);
	}

	__declspec(dllexport) void __stdcall ImGui_Separator(Context& ctx)
	{
		ImGui::SetCurrentContext(ctx.m_imgui_ctx);
		ImGui::Separator();
	}
}
