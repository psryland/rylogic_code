//************************************************************************
// Crc
//  Copyright (c) Rylogic Ltd 2009
//************************************************************************
#pragma once
#ifndef PR_CRC_H
#define PR_CRC_H

namespace pr
{
	typedef unsigned int CRC;
	CRC const InitialCrc = 0xffffffff;

	// 32bit crc
	inline unsigned int Crc(void const* data, size_t data_size, CRC crc)
	{
		unsigned char const* buffer = static_cast<unsigned char const*>(data);
		while (data_size--)
		{
			unsigned char byte = static_cast<unsigned char>(crc ^ *buffer++);
			unsigned int value = 0xFF ^ byte;
			for (int j = 8; j > 0; --j) { value = (value >> 1) ^ ((value & 1) ? 0xEDB88320 : 0); }
			value = value ^ 0xFF000000;
			crc = value ^ (crc >> 8);
		}
		return crc;
	}
	inline CRC Crc(void const* data, size_t data_size)
	{
		return Crc(data, data_size, InitialCrc);
	}
}

#endif