//*******************************************************
// mergebin
// Written by Robert Simpson (robert@blackcastlesoft.com)
//
// Released to the public domain, use at your own risk!
//*******************************************************

#pragma once
#include "mergebin/forward.h"
#include "mergebin/meta_data_tables.h"
#include "mergebin/table_data.h"

CMetadataTables::CMetadataTables(CMetadata& metaData)
	:m_meta(metaData)
{
	CMetadata::CStream *ps = m_meta.GetStream("#~");
	if (!ps) throw;

	*static_cast<CMetadata::CStream *>(this) = *ps;

	LPBYTE pb = m_pbData + sizeof(DWORD);
	m_pbMajorVersion = pb;
	m_pbMinorVersion = m_pbMajorVersion + 1;
	m_pbHeapOffsetSizes = m_pbMinorVersion + 1;
	// Skip a byte
	m_pullMaskValid = (UINT64 *)(m_pbHeapOffsetSizes + 2);
	m_pullMaskSorted = m_pullMaskValid + 1;

	m_pdwTableLengths = (LPDWORD)(m_pullMaskSorted + 1);

	m_dwTables = 0;
	for (int n = 0; n < 64; n++)
	{
		if ((((*m_pullMaskValid) >> n) & 1) == 1)
		{
			m_pdwTableLengthIndex[n] = &m_pdwTableLengths[m_dwTables++];
		}
		else
		{
			m_pdwTableLengthIndex[n] = NULL;
		}
	}
	m_pbData = (LPBYTE)(m_pdwTableLengths + m_dwTables);

	for (int n = 0; n < 64; n++)
	{
		if (m_pdwTableLengthIndex[n] && g_arTableTypes[n])
		{
			m_pTables[n] = g_arTableTypes[n](this);
		}
		else
			m_pTables[n] = 0;
	}
}
CMetadataTables::~CMetadataTables()
{
	for (int n = 0; n < 64; n++)
	{
		if (m_pTables[n])
			delete m_pTables[n];
	}
}
