//******************************************************
// Web Get
//  Copyright (c) Rylogic Ltd 2009
//******************************************************

#pragma once

#include <string>
#include <fstream>
#include <filesystem>
#include <wininet.h>
#pragma comment(lib, "wininet.lib")

namespace pr::network
{
	struct InternetHandle
	{
		HINTERNET m_hinet;
		InternetHandle(HINTERNET hinet)
			:m_hinet(hinet)
		{}
		~InternetHandle()
		{
			if (m_hinet)
				InternetCloseHandle(m_hinet);
		}
		operator HINTERNET() const
		{
			return m_hinet;
		}
	};

	// Read a file from the internet
	template <typename Out>
	bool WebGet(char const* url, Out& out)
	{
		InternetHandle hinet = InternetOpen("WebGet", INTERNET_OPEN_TYPE_PRECONFIG, 0, 0, 0);
		if (!hinet)
			return false;

		InternetHandle hurl  = InternetOpenUrl(hinet, url, 0, 0, INTERNET_FLAG_RELOAD|INTERNET_FLAG_PRAGMA_NOCACHE|INTERNET_FLAG_NO_CACHE_WRITE, 0);
		if (!hurl)
			return false;

		for(;;)
		{
			char buf[512]; DWORD read;
			if (!InternetReadFile(hurl, buf, sizeof(buf), &read)) return false;
			if (read == 0) return true;
			out(buf, read);
		}
	}

	// Read a text file from a URL, copying the text into 'data'
	// Returns true if a file was successfully read from 'url'
	inline bool WebGet(char const* url, std::string& data)
	{
		struct StrOut
		{
			std::string* m_str;
			explicit StrOut(std::string& str)
				:m_str(&str)
			{}
			void operator()(char const* buf, DWORD size)
			{
				m_str->append(buf, size);
			}
		};

		StrOut out(data);
		return WebGet(url, out);
	}

	// Read a file from a URL saving it to 'filename'
	inline bool WebGet(char const* url, std::filesystem::path const& filename)
	{
		struct FileOut
		{
			std::ofstream m_file;
			explicit FileOut(std::filesystem::path const& filename)
				:m_file(filename)
			{}
			void operator()(char const* buf, DWORD size)
			{
				m_file.write(buf, size);
			}
		};

		FileOut out(filename);
		return WebGet(url, out);
	}
}









//		
//#define UPDATECHECK_BROWSER_STRING _T("Update search")
//		class CUpdateCheck  
//		{
//		public:
//			virtual void Check(UINT uiURL);
//			virtual void Check(const CString& strURL);
//			CUpdateCheck();
//			virtual ~CUpdateCheck();
//
//			static HINSTANCE GotoURL(LPCTSTR url, int showcmd);
//			static BOOL GetFileVersion(DWORD &dwMS, DWORD &dwLS);
//			static LONG GetRegKey(HKEY key, LPCTSTR subkey, LPTSTR retdata);
//
//		protected:
//			virtual void MsgUpdateAvailable(DWORD dwMSlocal, DWORD dwLSlocal, DWORD dwMSWeb, DWORD dwLSWeb, const CString& strURL);
//			virtual void MsgUpdateNotAvailable(DWORD dwMSlocal, DWORD dwLSlocal);
//			virtual void MsgUpdateNoCheck(DWORD dwMSlocal, DWORD dwLSlocal);
//		};

//
//BOOL CUpdateCheck::GetFileVersion(DWORD &dwMS, DWORD &dwLS)
//{
//	char szModuleFileName[MAX_PATH];
//
//    LPBYTE  lpVersionData; 
//
//	if (GetModuleFileName(AfxGetInstanceHandle(), szModuleFileName, sizeof(szModuleFileName)) == 0) return FALSE;
//
//    DWORD dwHandle;     
//    DWORD dwDataSize = ::GetFileVersionInfoSize(szModuleFileName, &dwHandle); 
//    if ( dwDataSize == 0 ) 
//        return FALSE;
//
//    lpVersionData = new BYTE[dwDataSize]; 
//    if (!::GetFileVersionInfo(szModuleFileName, dwHandle, dwDataSize, (void**)lpVersionData) )
//    {
//		delete [] lpVersionData;
//        return FALSE;
//    }
//
//    ASSERT(lpVersionData != NULL);
//
//    UINT nQuerySize;
//	VS_FIXEDFILEINFO* pVsffi;
//    if ( ::VerQueryValue((void **)lpVersionData, _T("\\"),
//                         (void**)&pVsffi, &nQuerySize) )
//    {
//		dwMS = pVsffi->dwFileVersionMS;
//		dwLS = pVsffi->dwFileVersionLS;
//		delete [] lpVersionData;
//        return TRUE;
//    }
//
//	delete [] lpVersionData;
//    return FALSE;
//
//}
//
//void CUpdateCheck::Check(UINT uiURL)
//{
//	CString strURL(MAKEINTRESOURCE(uiURL));
//	Check(strURL);
//}
//
//void CUpdateCheck::Check(const CString& strURL)
//{
//	DWORD dwMS, dwLS;
//	if (!GetFileVersion(dwMS, dwLS))
//	{
//		ASSERT(FALSE);
//		return;
//	}
//
//	CWaitCursor wait;
//	HINTERNET hInet = InternetOpen(UPDATECHECK_BROWSER_STRING, INTERNET_OPEN_TYPE_PRECONFIG, NULL, NULL, NULL);
//	HINTERNET hUrl = InternetOpenUrl(hInet, strURL, NULL, -1L,
//										 INTERNET_FLAG_RELOAD | INTERNET_FLAG_PRAGMA_NOCACHE |
//										 INTERNET_FLAG_NO_CACHE_WRITE |WININET_API_FLAG_ASYNC, NULL);
//	if (hUrl)
//	{
//		char szBuffer[512];
//		DWORD dwRead;
//		if (InternetReadFile(hUrl, szBuffer, sizeof(szBuffer), &dwRead))
//		{
//			if (dwRead > 0)
//			{
//				szBuffer[dwRead] = 0;
//				CString strSubMS1;
//				CString strSubMS2;
//				CString strSub;
//				DWORD dwMSWeb;
//				AfxExtractSubString(strSubMS1, szBuffer, 0, '|');
//				AfxExtractSubString(strSubMS2, szBuffer, 1, '|');
//				dwMSWeb = MAKELONG((WORD) atol(strSubMS2), (WORD) atol(strSubMS1));
//
//				if (dwMSWeb > dwMS)
//				{
//					AfxExtractSubString(strSub, szBuffer, 2, '|');
//					MsgUpdateAvailable(dwMS, dwLS, dwMSWeb, 0, strSub);
//				}
//				else
//					MsgUpdateNotAvailable(dwMS, dwLS);
//			}
//			else
//				MsgUpdateNoCheck(dwMS, dwLS);
//
//		}
//		InternetCloseHandle(hUrl);
//	}
//	else
//		MsgUpdateNoCheck(dwMS, dwLS);
//
//	InternetCloseHandle(hInet);
//}
//
//HINSTANCE CUpdateCheck::GotoURL(LPCTSTR url, int showcmd)
//{
//    TCHAR key[MAX_PATH + MAX_PATH];
//
//    // First try ShellExecute()
//    HINSTANCE result = ShellExecute(NULL, _T("open"), url, NULL,NULL, showcmd);
//
//    // If it failed, get the .htm regkey and lookup the program
//    if ((UINT)result <= HINSTANCE_ERROR) 
//	{
//
//        if (GetRegKey(HKEY_CLASSES_ROOT, _T(".htm"), key) == ERROR_SUCCESS) 
//		{
//            lstrcat(key, _T("\\shell\\open\\command"));
//
//            if (GetRegKey(HKEY_CLASSES_ROOT,key,key) == ERROR_SUCCESS) 
//			{
//                TCHAR *pos;
//                pos = _tcsstr(key, _T("\"%1\""));
//                if (pos == NULL) {                     // No quotes found
//                    pos = _tcsstr(key, _T("%1"));      // Check for %1, without quotes 
//                    if (pos == NULL)                   // No parameter at all...
//                        pos = key+lstrlen(key)-1;
//                    else
//                        *pos = '\0';                   // Remove the parameter
//                }
//                else
//                    *pos = '\0';                       // Remove the parameter
//
//                lstrcat(pos, _T(" "));
//                lstrcat(pos, url);
//
//                result = (HINSTANCE) WinExec(key,showcmd);
//            }
//        }
//    }
//
//    return result;
//}
//
//LONG CUpdateCheck::GetRegKey(HKEY key, LPCTSTR subkey, LPTSTR retdata)
//{
//    HKEY hkey;
//    LONG retval = RegOpenKeyEx(key, subkey, 0, KEY_QUERY_VALUE, &hkey);
//
//    if (retval == ERROR_SUCCESS) 
//	{
//        long datasize = MAX_PATH;
//        TCHAR data[MAX_PATH];
//        RegQueryValue(hkey, NULL, data, &datasize);
//        lstrcpy(retdata,data);
//        RegCloseKey(hkey);
//    }
//
//    return retval;
//}
//
//
//void CUpdateCheck::MsgUpdateAvailable(DWORD dwMSlocal, DWORD dwLSlocal, DWORD dwMSWeb, DWORD dwLSWeb, const CString& strURL)
//{
//	CString strMessage;
//	strMessage.Format(IDS_UPDATE_AVAILABLE, HIWORD(dwMSlocal), LOWORD(dwMSlocal), HIWORD(dwMSWeb), LOWORD(dwMSWeb));
//
//	if (AfxMessageBox(strMessage, MB_YESNO|MB_ICONINFORMATION) == IDYES)
//		GotoURL(strURL, SW_SHOW);
//}
//
//void CUpdateCheck::MsgUpdateNotAvailable(DWORD dwMSlocal, DWORD dwLSlocal)
//{
//	AfxMessageBox(IDS_UPDATE_NO, MB_OK|MB_ICONINFORMATION);
//}
//
//void CUpdateCheck::MsgUpdateNoCheck(DWORD dwMSlocal, DWORD dwLSlocal)
//{
//	AfxMessageBox(IDS_UPDATE_NOCHECK, MB_OK|MB_ICONINFORMATION);
//}
