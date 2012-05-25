//*******************************************************
//
//	Defiler - (c) Paul Ryland Jan 2005
//
//*******************************************************

#include "Defiler.h"
#include "Common/StdAlgorithm.h"

//*****
// Returns true if 'c' is a format string identifier
bool IsFormatId(char c)
{
	return	c == 'c' || c == 'C' || c == 'd' || c == 'i' ||
			c == 'o' || c == 'u' || c == 'x' || c == 'X' ||
			c == 'e' || c == 'E' || c == 'f' || c == 'g' ||
			c == 'G' || c == 'n' || c == 'p' || c == 's' || c == 'S';
}

//*****
// Return the type of 'c'
Variant::Type GetFormatIdType(char c)
{
	switch( c )
	{
	case 'c': case 'C':												return Variant::Char;
	case 'd': case 'i': case 'o': case 'u': case 'x': case 'X':		return Variant::Int;
	case 'f':														return Variant::Float;
	case 'e': case 'E': case 'g': case 'G':							return Variant::Double;
	case 'n': case 'p':												return Variant::Pointer;
	case 's': case 'S':												return Variant::String;
	default:	PR_ERROR_STR("Unknown type");						return Variant::Unknown;
	};
}

//*****
// Display usage
int OnExit(ErrorCode error_code)
{
	if( error_code == ErrorCode_Success ) return 0;
	printf(	"*************************************\n"
			"* Defiler    - (c) Paul Ryland 2005 *\n"
			"*************************************\n");
	switch( error_code )
	{
	case ErrorCode_Success:					break;
	case ErrorCode_InvalidArgs:				printf(" *** Invalid argments\n");				break;
	case ErrorCode_CommandScriptNotFound:	printf(" *** Failed to load command script\n");	break;
	case ErrorCode_CommandScriptParseError:	printf(" *** Parse error in command script\n"); break;
	case ErrorCode_FailedToOpenInputFile:	printf(" *** Failed to open input file\n");		break;
	case ErrorCode_FailedToOpenOutputFile:	printf(" *** Failed to open output file\n");	break;
	case ErrorCode_NoInputFile:				printf(" *** No input filename was given\n");	break;
	case ErrorCode_NoOutputFile:			printf(" *** No output filename was given\n");	break;
	case ErrorCode_TooManyVars:				printf(" *** Too many variables used\n");		break;
	case ErrorCode_IncompleteRead:			printf(" *** Incomplete read\n");				break;
	default: break;
	};
	printf(	"\n    Usage: Defiler \"command_script\"\n\n");
	return error_code;
}

#include <conio.h>

void Paulf(uint i, ...)
{
	va_list arglist;
    va_start(arglist, i);
	double f = va_arg(arglist, double);
	va_end(arglist);

	printf("%f", f);
}

//*****
int main(int argc, char* argv[])
{
//	uint64 f;
//	scanf("%lf", &f);
//
//	Paulf(2, f);
//	getch();
//
//return 0;
	if( argc == 1 ) return OnExit(ErrorCode_ShowUsage);
	if( argc != 2 ) return OnExit(ErrorCode_InvalidArgs);

	Defiler defiler;
	return OnExit(defiler.Run(argv[1]));
}

//*********************************************************
// Defiler Implementation
//*****
// Do some defiling
ErrorCode Defiler::Run(const char* command_script_filename)
{
	// Load the command script
    if( !m_command_script.LoadFromFile(command_script_filename) )	return ErrorCode_CommandScriptNotFound;
	
	InitVars();
	return ParseCommon();
}

//*****
// Reset the variables and add a default variable
void Defiler::InitVars()
{
	m_var.clear();
	m_var.push_back(Var());
}

//*****
// Parse keywords that can occur at any level
ErrorCode Defiler::ParseCommon()
{
	std::string keyword;
	while( m_command_script.GetKeyword(keyword) )
	{
		ErrorCode result = ErrorCode_Success;
			 if( PR::Str::EqualsNoCase(keyword, "Loop")			) result = ParseLoop();
		else if( PR::Str::EqualsNoCase(keyword, "OutputFile")	) result = ParseOutputFile();
		else if( PR::Str::EqualsNoCase(keyword, "InputFile")	) result = ParseInputFile();
		else if( PR::Str::EqualsNoCase(keyword, "ReadLine")		) result = ParseReadLine();
		else if( PR::Str::EqualsNoCase(keyword, "Read")			) result = ParseRead();
		else if( PR::Str::EqualsNoCase(keyword, "Write")		) result = ParseWrite();
		if( result != ErrorCode_Success ) return result;
	}
	return ErrorCode_Success;
}

//*****
// Parse a loop command
ErrorCode Defiler::ParseLoop()
{
	const uint Infinite = 0xFFFFFFFF;
	uint loop_count;
	m_command_script.ExtractUInt(loop_count, 10);
	if( loop_count == 0 ) loop_count = Infinite;

	if( !m_command_script.FindSectionStart() )	return ErrorCode_CommandScriptParseError;
	uint script_pos = m_command_script.GetPosition();
	for( uint i = 0; i < loop_count || loop_count == Infinite; ++i )
	{
		m_command_script.SetPosition(script_pos);

		ErrorCode result = ParseCommon();
		if( result != ErrorCode_Success )		return result;
		if( m_input_file.IsEndOfFile() )		break;
	}
	if( !m_command_script.FindSectionEnd() )	return ErrorCode_CommandScriptParseError;
	return ErrorCode_Success;
}

//*****
// Open the file to read from
ErrorCode Defiler::ParseInputFile()
{
	std::string input_filename;
	if( !m_command_script.ExtractString(input_filename) )	return ErrorCode_CommandScriptParseError;
	if( !m_input_file.Open(input_filename.c_str(), "rt") )	return ErrorCode_FailedToOpenInputFile;
	return ErrorCode_Success;
}

//*****
// Open the file to write to
ErrorCode Defiler::ParseOutputFile()
{
	std::string output_filename;
	if( !m_command_script.ExtractString(output_filename) )	return ErrorCode_CommandScriptParseError;
	if( !m_output_file.Open(output_filename.c_str(), "wt") )	return ErrorCode_FailedToOpenOutputFile;
	return ErrorCode_Success;
}

//*****
// Read a line from 'InputFile'
ErrorCode Defiler::ParseReadLine()
{
	if( !m_input_file.IsOpen() )						return ErrorCode_NoInputFile;

	// Get the identifier for the line
	std::string id;
	m_command_script.ExtractIdentifier(id);
	if( id.length() == 0 )
	{
		printf("Missing identifier in *ReadLine\n");
		return ErrorCode_CommandScriptParseError;
	}

	// Try and find a var with the same id or add a var
	Var var(id, Variant::String);
	TVar::iterator var_iter = std::find(m_var.begin(), m_var.end(), var); 
	if( var_iter == m_var.end() ) { m_var.push_back(Var()); var_iter = m_var.end(); --var_iter; }

	// Set the id for the var
	var_iter->Init(id, Variant::String);

	// Read the line
	char* input_str = reinterpret_cast<char*>(var_iter->Set());
	fgets(input_str, Variant::Var::MAX_STRING_LENGTH, m_input_file);
	input_str = strstr(input_str, "\n");
	if( input_str ) *input_str = '\0';

	return ErrorCode_Success;
}

//*****
// Read a formatted string from 'InputFile'
ErrorCode Defiler::ParseRead()
{
	if( !m_input_file.IsOpen() )						return ErrorCode_NoInputFile;
	
	// Read the format string
	std::string read_fmt;
	if( !m_command_script.ExtractCString(read_fmt) )		return ErrorCode_CommandScriptParseError;
	PR::Str::Replace(read_fmt, "%f", "%lf");

	// Initialise pointers to vars that we'll read into
	TVar::iterator in[MaxVars];
	for( uint i = 0; i < MaxVars; ++i ) in[i] = m_var.begin();
	uint num_input_vars = 0;

	// Parse the format string
	uint length = (uint)read_fmt.length();
	for( uint i = 0; i < length; ++i )
	{
		if( read_fmt.c_str()[i] != '%' ) continue;
		++i;
		if( read_fmt.c_str()[i] == '%' ) continue;
		
		// Find the format type
		while( !IsFormatId(read_fmt.c_str()[i]) )
		{
			++i;
			if( isspace(read_fmt.c_str()[i]) )
			{
				printf("Unknown format identifier in *Read\n");
				return ErrorCode_CommandScriptParseError;
			}
		}
		Variant::Type type = GetFormatIdType(read_fmt.c_str()[i]);
		
		// Found a format identifier so read a variable		
		std::string id;
		m_command_script.ExtractIdentifier(id);
		if( id.length() == 0 )
		{
			printf("Missing variable for format identifier '%s' in *Read\n", read_fmt.c_str()[i]);
			return ErrorCode_CommandScriptParseError;
		}

		// Try and find a var with the same id or add a var
		Var var(id, type);
		TVar::iterator var_iter = std::find(m_var.begin(), m_var.end(), var); 
		if( var_iter == m_var.end() ) { m_var.push_back(Var()); var_iter = m_var.end(); --var_iter; }
		
		// Set the id for the var
		var_iter->Init(id, type);

		in[num_input_vars] = var_iter;
		++num_input_vars;
		if( num_input_vars >= MaxVars ) return ErrorCode_TooManyVars;
	}

	// Read from 'InputFile'
	uint num_read = fscanf(m_input_file, read_fmt.c_str(),
		in[ 0]->Set(), in[ 1]->Set(), in[ 2]->Set(), in[ 3]->Set(), in[ 4]->Set(), in[ 5]->Set(), in[ 6]->Set(), in[ 7]->Set(), in[ 8]->Set(), in[ 9]->Set(),
		in[10]->Set(), in[11]->Set(), in[12]->Set(), in[13]->Set(), in[14]->Set(), in[15]->Set(), in[16]->Set(), in[17]->Set(), in[18]->Set(), in[19]->Set(),
		in[20]->Set(), in[21]->Set(), in[22]->Set(), in[23]->Set(), in[24]->Set(), in[25]->Set(), in[26]->Set(), in[27]->Set(), in[28]->Set(), in[29]->Set(),
		in[30]->Set(), in[31]->Set(), in[32]->Set(), in[33]->Set(), in[34]->Set(), in[35]->Set(), in[36]->Set(), in[37]->Set(), in[38]->Set(), in[39]->Set(),
		in[40]->Set(), in[41]->Set(), in[42]->Set(), in[43]->Set(), in[44]->Set(), in[45]->Set(), in[46]->Set(), in[47]->Set(), in[48]->Set(), in[49]->Set());

	if( num_read != num_input_vars ) return ErrorCode_IncompleteRead;
	return ErrorCode_Success;
}

//*****
// Read a 'Write' command from the script
ErrorCode Defiler::ParseWrite()
{
	if( !m_output_file.IsOpen() )						return ErrorCode_NoOutputFile;

	// Read the format string
	std::string write_fmt;
	if( !m_command_script.ExtractCString(write_fmt) )	return ErrorCode_CommandScriptParseError;

	// Initialise pointers to vars that we'll read from
	TVar::iterator out[MaxVars];
	for( uint i = 0; i < MaxVars; ++i ) out[i] = m_var.begin();
	uint num_output_vars = 0;

	// Parse the format string
	uint length = (uint)write_fmt.length();
	for( uint i = 0; i < length; ++i )
	{
		if( write_fmt.c_str()[i] != '%' ) continue;
		++i;
		if( write_fmt.c_str()[i] == '%' ) continue;
		
		// Find the format type
		while( !IsFormatId(write_fmt.c_str()[i]) )
		{
			++i;
			if( isspace(write_fmt.c_str()[i]) )
			{
				printf("Unknown format identifier in *Write\n");
				return ErrorCode_CommandScriptParseError;
			}
		}
		Variant::Type type = GetFormatIdType(write_fmt.c_str()[i]);
		
		// Found a format identifier so read a variable		
		std::string id;
		m_command_script.ExtractIdentifier(id);
		if( id.length() == 0 )
		{
			printf("Missing variable for format identifier '%s' in *Write\n", write_fmt.c_str()[i]);
			return ErrorCode_CommandScriptParseError;
		}

		// Find the var
		Var var(id, type);
		TVar::iterator var_iter = std::find(m_var.begin(), m_var.end(), var); 
		if( var_iter == m_var.end() )
		{
			printf("Unknown parameter '%s' in *Write\n", id.c_str());
			return ErrorCode_CommandScriptParseError;
		}

		if( var_iter->m_type != type )
		{
			printf("Format identifier/variable type mismatch in *Write\n");
			return ErrorCode_CommandScriptParseError;
		}

		out[num_output_vars] = var_iter;
		++num_output_vars;
		if( num_output_vars >= MaxVars ) return ErrorCode_TooManyVars;
	}

	// Write to 'OutputFile'
	fprintf(m_output_file, write_fmt.c_str(),
		out[ 0]->Get(), out[ 1]->Get(), out[ 2]->Get(), out[ 3]->Get(), out[ 4]->Get(), out[ 5]->Get(), out[ 6]->Get(), out[ 7]->Get(), out[ 8]->Get(), out[ 9]->Get(),
		out[10]->Get(), out[11]->Get(), out[12]->Get(), out[13]->Get(), out[14]->Get(), out[15]->Get(), out[16]->Get(), out[17]->Get(), out[18]->Get(), out[19]->Get(),
		out[20]->Get(), out[21]->Get(), out[22]->Get(), out[23]->Get(), out[24]->Get(), out[25]->Get(), out[26]->Get(), out[27]->Get(), out[28]->Get(), out[29]->Get(),
		out[30]->Get(), out[31]->Get(), out[32]->Get(), out[33]->Get(), out[34]->Get(), out[35]->Get(), out[36]->Get(), out[37]->Get(), out[38]->Get(), out[39]->Get(),
		out[40]->Get(), out[41]->Get(), out[42]->Get(), out[43]->Get(), out[44]->Get(), out[45]->Get(), out[46]->Get(), out[47]->Get(), out[48]->Get(), out[49]->Get());

	return ErrorCode_Success;
}
