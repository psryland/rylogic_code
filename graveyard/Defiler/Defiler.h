//*******************************************************
//
//	Defiler - (c) Paul Ryland Jan 2005
//
//*******************************************************
#ifndef DEFILER_H
#define DEFILER_H

#include <stdio.h>
#include <windows.h>
#include "Common/StdList.h"
#include "Common/PRScript.h"
#include "Common/StdString.h"
#include "Common/PRFile.h"
#include "Common/Variant.h"
#include "Crypt/Crypt.h"

enum ErrorCode
{
	ErrorCode_Success = 0,
	ErrorCode_ShowUsage,
	ErrorCode_InvalidArgs,
	ErrorCode_CommandScriptNotFound,
	ErrorCode_CommandScriptParseError,
	ErrorCode_FailedToOpenInputFile,
	ErrorCode_FailedToOpenOutputFile,
	ErrorCode_NoInputFile,
	ErrorCode_NoOutputFile,
	ErrorCode_IncompleteRead,
	ErrorCode_TooManyVars,
	ErrorCode_NumberOf
};

struct Var
{
	Var() : m_id(""), m_crc(0), m_type(Variant::Unknown)	{ ZeroMemory(&m_value, sizeof(m_value)); }
	Var(const std::string& id, Variant::Type type)			{ Init(id, type); }
	
	void Init(const std::string& id, Variant::Type type)
	{
        m_id		= id;
		m_crc		= Crypt::Crc(id.c_str(), (uint)id.length());
		m_type		= type;
		ZeroMemory(&m_value, sizeof(m_value));
	}

	void* Set()
	{
		using namespace Variant;
		switch( m_type )
		{
		case Char:		return &m_value.m_char;
		case Int:		return &m_value.m_int;
		case Float:
		case Double:	return &m_value.m_double;
		case String:	return  m_value.m_string;
		case Pointer:	return &m_value.m_pointer;
		default:		return &m_value.m_int;
		};
	}
	const Variant::Var& Get()
	{
		return m_value;
		//using namespace Variant;
		//switch( m_type )
		//{
		//case Char:		return m_value.m_char		;
		//case Int:		return m_value.m_int		;
		//case Float:
		//case Double:	return m_value.m_double		;
		//case String:	return m_value.m_string		;
		//case Pointer:	return m_value.m_pointer	;
		//default:		return m_value.m_int		;
		//};
	}

	std::string		m_id;
	Crypt::CRC		m_crc;
	Variant::Type	m_type;
	Variant::Var	m_value;
};
bool operator == (const Var& a, const Var& b)	{ return a.m_crc == b.m_crc; }

class Defiler
{
public:
	ErrorCode	Run(const char* command_script_filename);

private:
	void		InitVars		();
	ErrorCode	ParseCommon		();
	ErrorCode	ParseLoop		();
	ErrorCode	ParseInputFile	();
	ErrorCode	ParseOutputFile	();
	ErrorCode	ParseReadLine	();
	ErrorCode	ParseRead		();
	ErrorCode	ParseWrite		();

private:
	enum { AsManyAsPossible = 0x7FFFFFFF, MaxVars = 50 };
	typedef std::list<Var>	TVar;

	PR::ScriptLoader	m_command_script;
	PR::File			m_input_file;
	PR::File			m_output_file;
	TVar				m_var;
};

#endif//DEFILER_H