//********************************************************************
//
//	An 3DS-Max "ASE" to "DirectX" file converter
//
//********************************************************************

#ifndef ASE2X_H
#define ASE2X_H

#include "Common\PRString.h"

class Ase2X
{
public:
	void	ShowHelp() const;
	bool	ParseOptions(int argc, char* argv[]);
	bool	Convert();

private:
	bool	Error(const char* str);
	bool	Msg(const char* str);
	bool	Info(const char* str);

private:
	std::string		m_source_filename;
	std::string		m_output_filename;
	bool			m_verbose;
	bool			m_generate_normals;
	unsigned int	m_geometry_type;
};

#endif//ASE2X_H
