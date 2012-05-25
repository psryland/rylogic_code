//**********************************************
//
//	A program for converting binary to base64 ascii
//
//**********************************************

#include <atlenc.h>
#include "pr/common/assert.h"
#include "pr/common/command_line.h"
#include "pr/common/fmt.h"
#include "pr/common/byte_data.h"
#include "pr/str/prstring.h"
#include "pr/common/base64.h"
#include "pr/filesys/fileex.h"
#include "pr/maths/maths.h"

using namespace pr;
using namespace pr::cmdline;

class Main : public IOptionReceiver
{
public:
	Main();
	~Main();
	int  Run(int argc, char* argv[]);
	void ShowHelp();
	bool CmdLineOption(std::string const& option, TArgIter& arg, TArgIter arg_end);
	void Encode();
	void Decode();

private:
	enum { BlockSize = 65535 };
	bool		m_encode;
	std::string m_in_filename;
	std::string m_out_filename;
};

// Entry point *********************************************************************************************

int main(int argc, char* argv[])
{
	Main m;
	return m.Run(argc, argv);
}

// Main *********************************************************************************************

//*****
Main::Main()
:m_encode(true)
{}

//*****
Main::~Main()
{}

//*****
// Main program run
int Main::Run(int argc, char* argv[])
{
	if( !EnumCommandLine(argc, argv, *this) )	{ ShowHelp(); return -1; }
	if( m_in_filename.empty() )					{ ShowHelp(); return -1; }
	if( m_out_filename.empty() )
	{
		if( m_encode )	m_out_filename = m_in_filename + ".txt";
		else			m_out_filename = m_in_filename + ".bin";
	}

	if( m_encode )	Encode();
	else			Decode();
	return 0;
}

//*****
void Main::ShowHelp()
{
	printf(	"\n"
			"**************************************************\n"
			" --- Base64Encoder - Copyright © Rylogic 2006 --- \n"
			"**************************************************\n"
			"\n"
			"  Syntax: Base64Encoder <-enc|-dec> filename -O output_filename\n"
			"    -enc : Encode a file using base64 ascii encoding\n"
			"    -dec : Decode a base64 ascii encoded file\n"
			"    -O   : The name of the file to create (Default: filename.<bin|txt>)\n"
			"\n"
			);
}

//*****
bool Main::CmdLineOption(std::string const& option, TArgIter& arg, TArgIter arg_end)
{
	if( str::EqualI(option, "-enc") && arg != arg_end )	{ m_encode = true;  m_in_filename = *arg++; return true; }
	if( str::EqualI(option, "-dec") && arg != arg_end )	{ m_encode = false; m_in_filename = *arg++; return true; }
	if( str::EqualI(option, "-O"  ) && arg != arg_end )	{ m_out_filename = *arg++; return true; }
	printf("Error: Unknown option '%s'\n", option.c_str());
	return false;
}

//*****
void Main::Encode()
{
	pr::Handle in_file  = FileOpen(m_in_filename .c_str(), EFileOpen::Reading);
	pr::Handle out_file = FileOpen(m_out_filename.c_str(), EFileOpen::Writing);
	if (in_file  == INVALID_HANDLE_VALUE) { printf("Failed to open input file: %s\n" , m_in_filename .c_str()); return; }
	if (out_file == INVALID_HANDLE_VALUE) { printf("Failed to open output file: %s\n", m_out_filename.c_str()); return; }

	BYTE in_data[BlockSize];
	pr::ByteCont out_data;

	// Read the data
	for (DWORD read, to_read = GetFileSize(in_file, 0); to_read; to_read -= read)
	{
		read = pr::Min(to_read, DWORD(BlockSize));
		if (!FileRead(in_file, &in_data[0], read)) { printf("Failed to read input data\n"); return; }

		// Find the size needed
		int dest_length = Base64EncodeGetRequiredLength((int)read, ATL_BASE64_FLAG_NOPAD|ATL_BASE64_FLAG_NOCRLF);
		out_data.resize(dest_length);

		// Encode the data
		if (Base64Encode(&in_data[0], (int)read, (CHAR*)&out_data[0], &dest_length, ATL_BASE64_FLAG_NOPAD|ATL_BASE64_FLAG_NOCRLF) != TRUE) { printf("Failed to encode data\n"); return; }

		// Write the data
		if (!FileWrite(out_file, &out_data[0], dest_length))	{ printf("Failed to write encoded data"); return; }
	}
}

void Main::Decode()
{
	pr::Handle in_file  = FileOpen(m_in_filename .c_str(), EFileOpen::Reading);
	pr::Handle out_file = FileOpen(m_out_filename.c_str(), EFileOpen::Writing);
	if (in_file  == INVALID_HANDLE_VALUE) { printf("Failed to open input file: %s\n" , m_in_filename .c_str()); return; }
	if (out_file == INVALID_HANDLE_VALUE) { printf("Failed to open output file: %s\n", m_out_filename.c_str()); return; }

	CHAR in_data[BlockSize + 1];
	ByteCont out_data;
		
	// Read the data
	for (DWORD read, to_read = GetFileSize(in_file, 0); to_read; to_read -= read)
	{
		read = pr::Min(to_read, DWORD(BlockSize));
		if (!FileRead(in_file, &in_data[0], read)) { printf("Failed to read input data\n"); return; }

		// Find the size needed
		int dest_length = Base64DecodeGetRequiredLength((int)read);
		out_data.resize(dest_length);

		// Decode the data
		if (Base64Decode(&in_data[0], read, &out_data[0], &dest_length) != TRUE) { printf("Failed to decode data\n"); return; };

		// Write the data
		if (!FileWrite(out_file, &out_data[0], dest_length)) { printf("Failed to write encoded data"); return; }
	}
}

