//*******************************************************
// mergebin
// Written by Robert Simpson (robert@blackcastlesoft.com)
//
// Released to the public domain, use at your own risk!
//*******************************************************
#pragma once
#include "src/forward.h"
#include "src/meta_data.h"

class CMetadataTables :public CMetadata::CStream
{
protected:
	friend CTableData;
	CMetadata& m_meta;
	BYTE*      m_pbMajorVersion;
	BYTE*      m_pbMinorVersion;
	BYTE*      m_pbHeapOffsetSizes;
	UINT64*    m_pullMaskValid;
	UINT64*    m_pullMaskSorted;
	DWORD*     m_pdwTableLengths;

	DWORD* m_pdwTableLengthIndex[64];
	DWORD  m_dwTables;

	CTableData* m_pTables[64];

public:

	CMetadataTables(CMetadata& metaData);
	virtual ~CMetadataTables();
	CMetadataTables(CMetadataTables const&) = delete;
	CMetadataTables& operator=(CMetadataTables const&) = delete;

	UINT GetStringIndexSize() const
	{
		return ((*m_pbHeapOffsetSizes) & 0x01) == 0 ? sizeof(WORD) : sizeof(DWORD);
	}
	UINT GetGuidIndexSize() const
	{
		return ((*m_pbHeapOffsetSizes) & 0x02) == 0 ? sizeof(WORD) : sizeof(DWORD);
	}
	UINT GetBlobIndexSize() const
	{
		return ((*m_pbHeapOffsetSizes) & 0x04) == 0 ? sizeof(WORD) : sizeof(DWORD);
	}
	DWORD GetMaxIndexSizeOf(UINT* puiTables) const
	{
		DWORD dwMaxRows = 0;
		DWORD *pdwLength;
		UINT uCount = 0;

		while (*puiTables)
		{
			uCount++;
			pdwLength = m_pdwTableLengthIndex[*puiTables];
			if (pdwLength)
				dwMaxRows = max(dwMaxRows, pdwLength[0]);

			puiTables++;
		}

		return (dwMaxRows > 0xFFFF) ? 4 : 2;
		//return (dwMaxRows > (ULONG)(2 << (16 - uCount))) ? 4 : 2;
	}

	CTableData const* GetTable(UINT uId) const
	{
		return m_pTables[uId];
	}
	DWORD const* TableRowCount(UINT uType) const
	{
		return m_pdwTableLengthIndex[uType];
	}
};
