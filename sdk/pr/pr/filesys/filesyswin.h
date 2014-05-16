//**********************************************
// File system functions requiring windows
//  Copyright (c) Rylogic Ltd 2007
//**********************************************
//	Pathname	= full path e.g. Drive:/path/path/file.ext
//	Drive		= the drive e.g. "P:"
//	Path		= the directory without the drive. leading '/', no trailing '/'. e.g. /Path/path
//	Directory	= the drive + path. no trailing '/'. e.g P:/Path/path
//	Extension	= the last string following a '.'
//	Filename	= file name including extension
//	FileTitle	= file name not including extension
//
// A full pathname = drive + path + "/" + filetitle + "." + extension
//
#pragma once
#ifndef PR_FILESYS_WIN_H
#define PR_FILESYS_WIN_H

#include <windows.h>
#include <shobjidl.h>
#include <shlguid.h>
#include "pr/common/hresult.h"
#include "pr/common/windows_com.h"
#include "pr/filesys/filesys.h"

namespace pr
{
	namespace filesys
	{
		namespace impl
		{
			// Resolve a shortcut filename into the actual filename
			template <typename T>
			std::string& ResolveShortcut(std::string& shortcut)
			{
				pr::InitCom com(pr::InitCom::NoThrow);
				if (pr::Failed(com.m_res)) return shortcut;

				// Get a pointer to the IShellLink interface. 
				IShellLink* psl; 
				if( pr::Succeeded(CoCreateInstance(CLSID_ShellLink, NULL, CLSCTX_INPROC_SERVER, IID_IShellLink, (LPVOID*)&psl)) )
				{ 
					// Get a pointer to the IPersistFile interface. 
					IPersistFile* ppf; 
					if( pr::Succeeded(psl->QueryInterface(IID_IPersistFile, (void**)&ppf)) )
					{ 
						// Ensure that the string is Unicode. 
						WCHAR wsz[MAX_PATH];
						MultiByteToWideChar(CP_ACP, 0, shortcut.c_str(), -1, wsz, MAX_PATH); 
			 
						// Load the shortcut. 
						if( pr::Succeeded(ppf->Load(wsz, STGM_READ)) )
						{ 
							// Resolve the link. 
							if( pr::Succeeded(psl->Resolve(0, 0)) )
							{ 
								// Get the path to the link target. 
								char szGotPath[MAX_PATH]; 
								WIN32_FIND_DATA wfd; 
								if( pr::Succeeded(psl->GetPath(szGotPath, MAX_PATH, (WIN32_FIND_DATA*)&wfd, SLGP_SHORTPATH)) )
								{
									// Get the description of the target. 
									char szDescription[MAX_PATH]; 
									if( pr::Succeeded(psl->GetDescription(szDescription, MAX_PATH)) )
									{
										shortcut = szGotPath;
									}
								}
							} 
						} 
						// Release the pointer to the IPersistFile interface. 
						ppf->Release(); 
					}
					// Release the pointer to the IShellLink interface. 
					psl->Release(); 
				}
				return shortcut;
			}

			///**********************************************************************
			//* Function......: CreateShortcut
			//* Parameters....: lpszFileName - string that specifies a valid file name
			//*          lpszDesc - string that specifies a description for a 
			//							 shortcut
			//*          lpszShortcutPath - string that specifies a path and 
			//									 file name of a shortcut
			//* Returns.......: S_OK on success, error code on failure
			//* Description...: Creates a Shell link object (shortcut)
			//**********************************************************************/
			//HRESULT CreateShortcut(/*in*/ LPCTSTR lpszFileName, 
			//					/*in*/ LPCTSTR lpszDesc, 
			//					/*in*/ LPCTSTR lpszShortcutPath)
			//{
			//	HRESULT hRes = E_FAIL;
			//	DWORD dwRet = 0;
			//	CComPtr<IShellLink> ipShellLink;
			//		// buffer that receives the null-terminated string 
			//		// for the drive and path
			//	TCHAR szPath[MAX_PATH];    
			//		// buffer that receives the address of the final 
			//		//file name component in the path
			//	LPTSTR lpszFilePart;    
			//	WCHAR wszTemp[MAX_PATH];
			//        
			//	// Retrieve the full path and file name of a specified file
			//	dwRet = GetFullPathName(lpszFileName, 
			//					   sizeof(szPath) / sizeof(TCHAR), 
			//					   szPath, &lpszFilePart);
			//	if (!dwRet)                                        
			//		return hRes;

			//	// Get a pointer to the IShellLink interface
			//	hRes = CoCreateInstance(CLSID_ShellLink,
			//							NULL, 
			//							CLSCTX_INPROC_SERVER,
			//							IID_IShellLink,
			//							(void**)&ipShellLink);

			//	if (SUCCEEDED(hRes))
			//	{
			//		// Get a pointer to the IPersistFile interface
			//		CComQIPtr<IPersistFile> ipPersistFile(ipShellLink);

			//		// Set the path to the shortcut target and add the description
			//		hRes = ipShellLink->SetPath(szPath);
			//		if (FAILED(hRes))
			//			return hRes;

			//		hRes = ipShellLink->SetDescription(lpszDesc);
			//		if (FAILED(hRes))
			//			return hRes;

			//		// IPersistFile is using LPCOLESTR, so make sure 
			//				// that the string is Unicode
			//#if !defined _UNICODE
			//		MultiByteToWideChar(CP_ACP, 0, 
			//					   lpszShortcutPath, -1, wszTemp, MAX_PATH);
			//#else
			//		wcsncpy(wszTemp, lpszShortcutPath, MAX_PATH);
			//#endif

			//		// Write the shortcut to disk
			//		hRes = ipPersistFile->Save(wszTemp, TRUE);
			//	}

			//	return hRes;
			//}
		}//namespace impl

		inline std::string& ResolveShortcut		(std::string&       shortcut)	{ return impl::ResolveShortcut<void>(shortcut); }
		inline std::string  ResolveShortcut		(std::string const& shortcut)	{ std::string str = shortcut; return impl::ResolveShortcut<void>(str); }
	}
}

#endif
