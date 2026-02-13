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
		ID3D12CommandQueue* m_cmd_queue;
		HWND m_hwnd;
		DXGI_FORMAT m_rtv_format;
		int m_num_frames_in_flight;
		float m_font_scale;
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

		// SRV descriptor callbacks for ImGui_ImplDX12_InitInfo
		static void SrvDescriptorAlloc(ImGui_ImplDX12_InitInfo* info, D3D12_CPU_DESCRIPTOR_HANDLE* out_cpu, D3D12_GPU_DESCRIPTOR_HANDLE* out_gpu)
		{
			*out_cpu = info->SrvDescriptorHeap->GetCPUDescriptorHandleForHeapStart();
			*out_gpu = info->SrvDescriptorHeap->GetGPUDescriptorHandleForHeapStart();
		}
		static void SrvDescriptorFree(ImGui_ImplDX12_InitInfo*, D3D12_CPU_DESCRIPTOR_HANDLE, D3D12_GPU_DESCRIPTOR_HANDLE)
		{
			// Nothing to do — we own the entire heap and release it in Cleanup
		}

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
				io.FontGlobalScale = args.m_font_scale > 0.0f ? args.m_font_scale : 1.0f;

				// Initialise platform backend
				ImGui_ImplWin32_Init(args.m_hwnd);

				// Initialise renderer backend using the new InitInfo struct
				ImGui_ImplDX12_InitInfo init_info = {};
				init_info.Device = m_device;
				init_info.CommandQueue = args.m_cmd_queue;
				init_info.NumFramesInFlight = args.m_num_frames_in_flight;
				init_info.RTVFormat = args.m_rtv_format;
				init_info.SrvDescriptorHeap = m_srv_heap;
				init_info.SrvDescriptorAllocFn = SrvDescriptorAlloc;
				init_info.SrvDescriptorFreeFn = SrvDescriptorFree;
				ImGui_ImplDX12_Init(&init_info);
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

	// Create a dll context
	__declspec(dllexport) Context* __stdcall ImGui_Initialise(InitArgs const& args, ErrorHandler error_cb)
	{
		try
		{
			std::lock_guard<std::mutex> lock(g_mutex);
			g_contexts.push_back(std::make_unique<Context>(args, error_cb));
			return g_contexts.back().get();
		}
		catch (std::exception const& ex)
		{
			error_cb(ex.what());
			return nullptr;
		}
	}

	// Release a dll context
	__declspec(dllexport) void __stdcall ImGui_Shutdown(Context* ctx)
	{
		try
		{
			std::lock_guard<std::mutex> lock(g_mutex);
			auto it = std::remove_if(g_contexts.begin(), g_contexts.end(), [ctx](auto const& p) { return p.get() == ctx; });
			g_contexts.erase(it, g_contexts.end());
		}
		catch (std::exception const& ex)
		{
			ctx->m_error_cb(ex.what());
		}
	}

	// Start a new imgui frame
	__declspec(dllexport) void __stdcall ImGui_NewFrame(Context& ctx)
	{
		try
		{
			ImGui::SetCurrentContext(ctx.m_imgui_ctx);
			ImGui_ImplDX12_NewFrame();
			ImGui_ImplWin32_NewFrame();
			ImGui::NewFrame();
		}
		catch (std::exception const& ex)
		{
			ctx.m_error_cb(ex.what());
		}
	}

	__declspec(dllexport) void __stdcall ImGui_Render(Context& ctx, ID3D12GraphicsCommandList* cmd_list)
	{
		try
		{
			ImGui::SetCurrentContext(ctx.m_imgui_ctx);
			ImGui::Render();

			// Set the descriptor heap for imgui's font texture
			ID3D12DescriptorHeap* heaps[] = { ctx.m_srv_heap };
			cmd_list->SetDescriptorHeaps(1, heaps);

			ImGui_ImplDX12_RenderDrawData(ImGui::GetDrawData(), cmd_list);
		}
		catch (std::exception const& ex)
		{
			ctx.m_error_cb(ex.what());
		}
	}

	__declspec(dllexport) bool __stdcall ImGui_WndProc(Context& ctx, HWND hwnd, UINT msg, WPARAM wparam, LPARAM lparam)
	{
		try
		{
			ImGui::SetCurrentContext(ctx.m_imgui_ctx);
			auto result = ImGui_ImplWin32_WndProcHandler(hwnd, msg, wparam, lparam);
			return result != 0;
		}
		catch (std::exception const& ex)
		{
			ctx.m_error_cb(ex.what());
			return false;
		}
	}

	__declspec(dllexport) void __stdcall ImGui_Text(Context& ctx, char const* text)
	{
		try
		{
			ImGui::SetCurrentContext(ctx.m_imgui_ctx);
			ImGui::TextUnformatted(text);
		}
		catch (std::exception const& ex)
		{
			ctx.m_error_cb(ex.what());
		}
	}

	__declspec(dllexport) bool __stdcall ImGui_BeginWindow(Context& ctx, char const* name, bool* p_open, int flags)
	{
		try
		{
			ImGui::SetCurrentContext(ctx.m_imgui_ctx);
			return ImGui::Begin(name, p_open, static_cast<ImGuiWindowFlags_>(flags));
		}
		catch (std::exception const& ex)
		{
			ctx.m_error_cb(ex.what());
			return false;
		}
	}

	__declspec(dllexport) void __stdcall ImGui_EndWindow(Context& ctx)
	{
		try
		{
			ImGui::SetCurrentContext(ctx.m_imgui_ctx);
			ImGui::End();
		}
		catch (std::exception const& ex)
		{
			ctx.m_error_cb(ex.what());
		}
	}

	__declspec(dllexport) void __stdcall ImGui_SetNextWindowPos(Context& ctx, float x, float y, int cond)
	{
		try
		{
			ImGui::SetCurrentContext(ctx.m_imgui_ctx);
			ImGui::SetNextWindowPos(ImVec2(x, y), static_cast<ImGuiCond>(cond));
		}
		catch (std::exception const& ex)
		{
			ctx.m_error_cb(ex.what());
		}
	}

	__declspec(dllexport) void __stdcall ImGui_SetNextWindowSize(Context& ctx, float w, float h, int cond)
	{
		try
		{
			ImGui::SetCurrentContext(ctx.m_imgui_ctx);
			ImGui::SetNextWindowSize(ImVec2(w, h), static_cast<ImGuiCond>(cond));
		}
		catch (std::exception const& ex)
		{
			ctx.m_error_cb(ex.what());
		}
	}

	__declspec(dllexport) void __stdcall ImGui_SetNextWindowBgAlpha(Context& ctx, float alpha)
	{
		try
		{
			ImGui::SetCurrentContext(ctx.m_imgui_ctx);
			ImGui::SetNextWindowBgAlpha(alpha);
		}
		catch (std::exception const& ex)
		{
			ctx.m_error_cb(ex.what());
		}
	}

	__declspec(dllexport) bool __stdcall ImGui_Checkbox(Context& ctx, char const* label, bool* v)
	{
		try
		{
			ImGui::SetCurrentContext(ctx.m_imgui_ctx);
			return ImGui::Checkbox(label, v);
		}
		catch (std::exception const& ex)
		{
			ctx.m_error_cb(ex.what());
			return false;
		}
	}

	__declspec(dllexport) bool __stdcall ImGui_SliderFloat(Context& ctx, char const* label, float* v, float v_min, float v_max)
	{
		try
		{
			ImGui::SetCurrentContext(ctx.m_imgui_ctx);
			return ImGui::SliderFloat(label, v, v_min, v_max);
		}
		catch (std::exception const& ex)
		{
			ctx.m_error_cb(ex.what());
			return false;
		}
	}

	__declspec(dllexport) bool __stdcall ImGui_Button(Context& ctx, char const* label)
	{
		try
		{
			ImGui::SetCurrentContext(ctx.m_imgui_ctx);
			return ImGui::Button(label);
		}
		catch (std::exception const& ex)
		{
			ctx.m_error_cb(ex.what());
			return false;
		}
	}

	__declspec(dllexport) void __stdcall ImGui_SameLine(Context& ctx, float offset_from_start_x, float spacing)
	{
		try
		{
			ImGui::SetCurrentContext(ctx.m_imgui_ctx);
			ImGui::SameLine(offset_from_start_x, spacing);
		}
		catch (std::exception const& ex)
		{
			ctx.m_error_cb(ex.what());
		}
	}

	__declspec(dllexport) void __stdcall ImGui_Separator(Context& ctx)
	{
		try
		{
			ImGui::SetCurrentContext(ctx.m_imgui_ctx);
			ImGui::Separator();
		}
		catch (std::exception const& ex)
		{
			ctx.m_error_cb(ex.what());
		}
	}
	__declspec(dllexport) void __stdcall ImGui_PlotLines(Context& ctx, char const* label, float const* values, int values_count, int values_offset, char const* overlay_text, float scale_min, float scale_max, float graph_w, float graph_h)
	{
		try
		{
			ImGui::SetCurrentContext(ctx.m_imgui_ctx);
			ImGui::PlotLines(label, values, values_count, values_offset, overlay_text, scale_min, scale_max, ImVec2(graph_w, graph_h));
		}
		catch (std::exception const& ex)
		{
			ctx.m_error_cb(ex.what());
		}
	}
}
