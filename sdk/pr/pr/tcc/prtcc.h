//***************************************************************************************************
// Ldr Object Manager
//  (c)opyright Rylogic Ltd 2009
//***************************************************************************************************

#ifndef PR_TCC_H
#define PR_TCC_H
#pragma once

#ifndef _WIN64

#include <tcc.h>
#include <libtcc.h>
#include <algorithm>
#include <exception>

namespace pr
{
	namespace tcc
	{
		namespace EResult
		{
			enum Type
			{
				Success = 0,
				Failed = 0x80000000,
				CompileError,
				EntryPointNotFound,
				RelocateImageError,
				AddSymbolFailed,
				AddFileFailed,
				FailedToCreateTccState,
			};
		}
		namespace EOutput
		{
			enum Type
			{
				Memory      = TCC_OUTPUT_MEMORY,       // output will be ran in memory (no output file) (default)
				Exe         = TCC_OUTPUT_EXE,          // executable file
				Dll         = TCC_OUTPUT_DLL,          // dynamic library
				Obj         = TCC_OUTPUT_OBJ,          // object file
				PreProcess  = TCC_OUTPUT_PREPROCESS,   // preprocessed file (used internally)
			};
		}
		namespace EOutputFormat
		{
			enum Type
			{
				Elf         = TCC_OUTPUT_FORMAT_ELF,    // default output format: ELF
				Bin         = TCC_OUTPUT_FORMAT_BINARY, // binary image output
				Coff        = TCC_OUTPUT_FORMAT_COFF,   // COFF
			};
		}

		typedef void (*ReportFunc)(void* ctx, char const* msg);
		typedef std::auto_ptr<unsigned char> BinImage;

		// Wrapper around a compiled C program
		template <typename EntryFunc> struct Program
		{
			typedef EntryFunc entry_func_t;
			BinImage m_bin; // Memory containing the binary image
			EntryFunc run;  // Program entry point
		};

		// C++ wrapper around the tcclib interface
		class Compiler
		{
			TCCState* m_state;

			// Default error callback function
			static void ErrorFunc(void*, char const*) {}

		public:
			Compiler(EOutput::Type output = EOutput::Memory, EOutputFormat::Type format = EOutputFormat::Bin, ReportFunc report = 0, void* ctx = 0, bool enable_debug = false)
			:m_state(tcc_new())
			{
				if (m_state == 0) throw EResult::FailedToCreateTccState;

				// Set the output format
				m_state->output_format = format;

				// Set a default report function
				SetReportFunc(ctx, report != 0 ? report : ErrorFunc);

				// Tell tcc to compile to a memory image
				tcc_set_output_type(m_state, output);

				// Set the error callback function used to report compile errors/warnings
				tcc_set_error_func(m_state, ctx, report);

				// Add debug information in the generated code
				if (enable_debug) {}//tcc_enable_debug(m_state);
			}

			~Compiler()
			{
				// Clean up
				tcc_delete(m_state);
			}

			// Set the error callback function used to report compile errors/warnings
			void SetReportFunc(void* ctx, ReportFunc report)
			{
				tcc_set_error_func(m_state, ctx, report);
			}

			// Compile a string containing C source returning a binary image
			template <typename ProgType> void build(char const* code, char const* entry_point, ProgType& prog)
			{
				// Compile the source
				if (tcc_compile_string(m_state, code) != 0)
					throw EResult::CompileError;

				// Allocate memory for the binary image
				int bytes_needed = tcc_relocate(m_state, 0);
				if (bytes_needed == -1) throw EResult::RelocateImageError;
				prog.m_bin.reset(new unsigned char[bytes_needed]);

				// Relocate the binary into the provided memory
				tcc_relocate(m_state, prog.m_bin.get());

				// Find the entry point
				prog.run = (ProgType::entry_func_t)tcc_get_symbol(m_state, entry_point);
				if (prog.run == 0) throw EResult::EntryPointNotFound;
			}

			// Build and run code
			int run(char const* code) { return run(code, 0, 0); }
			int run(char const* code, int argc, char* argv[])
			{
				// Compile the source
				if (tcc_compile_string(m_state, code) != 0)
					throw EResult::CompileError;

				// Execute it
				return tcc_run(m_state, argc, argv);
			}

			// Add symbols available to the compiled code
			void add_symbol(char const* symbol_name, void* symbol)
			{
				if (tcc_add_symbol(m_state, symbol_name, symbol) == -1)
					throw EResult::AddSymbolFailed;
			}

			// Add a file to the compiled code
			void add_file(char const* filepath)
			{
				if (tcc_add_file(m_state, filepath) == -1)
					throw EResult::AddFileFailed;
			}
			
		};
	}
}
#endif //_WIN64

#endif