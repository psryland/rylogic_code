//*******************************************************
// mergebin
// Written by Robert Simpson (robert@blackcastlesoft.com)
//
// Released to the public domain, use at your own risk!
//*******************************************************

#pragma once

#include "mergebin/forward.h"

class CMetadata
{
public:
	class CStream
	{
	protected:
		friend CMetadata;
		DWORD* m_pdwOffset;
		DWORD* m_pdwSize;
		LPSTR  m_pszName;
		LPBYTE m_pbData;

	public:
		operator LPBYTE() const { return m_pbData; }
	};

protected:

	pr::PEFile& m_peFile;
	DWORD*      m_pdwSignature;
	WORD*       m_pwMajorVersion;
	WORD*       m_pwMinorVersion;
	DWORD*      m_pdwVersionLength;
	LPSTR       m_pszVersion;
	WORD*       m_pwStreams;
	CStream*    m_pStreams;

public:

	CMetadata(pr::PEFile& peFile)
		:m_peFile(peFile)
	{
		PIMAGE_COR20_HEADER pCor = m_peFile;
		if (!pCor) throw;

		auto pb = (BYTE*)m_peFile.PtrFromRVA(pCor->MetaData.VirtualAddress);
		if (!pb) throw;

		m_pdwSignature     = (LPDWORD)pb;
		m_pwMajorVersion   = (LPWORD)(m_pdwSignature + 1);
		m_pwMinorVersion   = m_pwMajorVersion + 1;
		m_pdwVersionLength = (LPDWORD)(m_pwMinorVersion + 3);
		m_pszVersion       = (LPSTR)(m_pdwVersionLength + 1);

		auto pbRoot = pb;
		pb = (LPBYTE)m_pszVersion;
		auto x = *m_pdwVersionLength;
		if (x % 4) x += 4 - (x % 4);
		pb += x;
		pb += 2;

		m_pwStreams = (LPWORD)pb;
		m_pStreams = new CStream[*m_pwStreams];
		pb = (LPBYTE)(m_pwStreams + 1);

		for (WORD n = 0; n < *m_pwStreams; n++)
		{
			m_pStreams[n].m_pdwOffset = (LPDWORD)pb;
			m_pStreams[n].m_pdwSize = m_pStreams[n].m_pdwOffset + 1;
			m_pStreams[n].m_pszName = (LPSTR)(m_pStreams[n].m_pdwSize + 1);
			m_pStreams[n].m_pbData = pbRoot + (*m_pStreams[n].m_pdwOffset);

			auto y = strlen(m_pStreams[n].m_pszName) + 1;
			if (y % 4) y += 4 - (y % 4);

			pb = (LPBYTE)m_pStreams[n].m_pszName + y;
		}
	}
	virtual ~CMetadata(void)
	{
		delete[] m_pStreams;
	}
	CMetadata(CMetadata const&) = delete;
	CMetadata& operator=(CMetadata const&) = delete;

	operator pr::PEFile&() const
	{
		return m_peFile;
	}

	WORD* StreamCount() const
	{
		return m_pwStreams;
	}

	CStream* GetStream(UINT uiStream)
	{
		if (uiStream >= *m_pwStreams) return nullptr;
		return &m_pStreams[uiStream];
	}
	CStream* GetStream(LPCSTR pszStreamName)
	{
		for (WORD n = 0; n < *m_pwStreams; n++)
		{
			if (_stricmp(pszStreamName, m_pStreams[n].m_pszName) == 0)
				return &m_pStreams[n];
		}
		return nullptr;
	}
};
