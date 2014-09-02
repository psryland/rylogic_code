// **********************************************************************
// Registry key
//  Copyright (c) Rylogic Ltd 2009
// **********************************************************************

#ifndef PR_REGISTRY_KEY_H
#define PR_REGISTRY_KEY_H

#include <windows.h>

namespace pr
{
	// The Key. Note: to nest keys pass this object to the 'Open' method (3rd parameter)
	class RegistryKey
	{
		HKEY	m_hkey;
		bool	m_valid;
	
	public:
		enum Access { Readonly = KEY_READ, Writeable = KEY_WRITE };

		RegistryKey() :	m_valid(false), m_hkey(NULL) {}
		~RegistryKey()	{ Close(); }

		bool Open(const char* key_name, Access access, HKEY key = HKEY_CURRENT_USER);
		void Close();

		bool Read(const char* key, bool&  value);
		bool Read(const char* key, DWORD& value);
		bool Read(const char* key, float& value);
		bool Read(const char* key, char* string, DWORD length);
		bool Read(const char* key, void* value, DWORD length);

		bool Write(const char* key, bool value);
		bool Write(const char* key, DWORD value);
		bool Write(const char* key, float value);
		bool Write(const char* key, const char* string);
		bool Write(const char* key, const void* data, DWORD length);

		DWORD GetKeyLength(const char* key);
		operator HKEY () { return m_hkey; }
	};

	//******************************************************************************
	// Implementation
	//*****
	// Open the registry key
	inline bool RegistryKey::Open(const char* key_name, Access access, HKEY key)
	{
		Close();
		DWORD was_created;
		m_valid = RegCreateKeyEx(key, key_name, 0, NULL, REG_OPTION_NON_VOLATILE, access, NULL, &m_hkey, &was_created) == ERROR_SUCCESS;
		if( access == Readonly && was_created == REG_CREATED_NEW_KEY )
		{
			RegDeleteKey(key, key_name);
			Close();
			return false;
		}
		return m_valid;
	}

	//*****
	// Close the registry key
	inline void RegistryKey::Close()
	{
		if( m_valid ) RegCloseKey(m_hkey);
		m_valid	= false;
	}

	//*****
	// Read a bool from the registry key. Returns true for success
	inline bool RegistryKey::Read(const char* key, bool& value)
	{
		if( !m_valid ) return false;
		
		DWORD data, length = sizeof(data), type = REG_DWORD;
		if( RegQueryValueEx(m_hkey, key, NULL, &type, (unsigned char*)&data, &length) != ERROR_SUCCESS ) return false;

		value = data > 0;
		return true;
	}

	//*****
	// Read a DWORD from the registry key. Returns true for success
	inline bool RegistryKey::Read(const char* key, DWORD& value)
	{
		if( !m_valid ) return false;

		DWORD data, length = sizeof(data), type = REG_DWORD;
		if( RegQueryValueEx(m_hkey, key, NULL, &type, (unsigned char*)&data, &length) != ERROR_SUCCESS ) return false;

		value = data;
		return true;
	}

	//*****
	// Read a float from the registry key. Returns true for success
	inline bool RegistryKey::Read(const char* key, float& value)
	{
		if( !m_valid ) return false;

		char data[256];
		DWORD length = sizeof(data), type = REG_SZ;
		if( RegQueryValueEx(m_hkey, key, NULL, &type, (unsigned char*)data, &length) != ERROR_SUCCESS ) return false;

		value = static_cast<float>(atof(data));
		return true;
	}

	//*****
	// Read a string from the registry key. Returns true for success
	inline bool RegistryKey::Read(const char* key, char* string, DWORD length)
	{
		if( !m_valid ) return false;

		DWORD type = REG_SZ;
		if( RegQueryValueEx(m_hkey, key, NULL, &type, (unsigned char*)string, &length) != ERROR_SUCCESS ) return false;
		return true;
	}

	//*****
	// Read raw data from the registry key. Returns true for success
	inline bool RegistryKey::Read(const char* key, void* value, DWORD length)
	{
		if( !m_valid ) return false;

		DWORD type = REG_BINARY;
		if( RegQueryValueEx(m_hkey, key, NULL, &type, (unsigned char*)value, &length) != ERROR_SUCCESS ) return false;
		return true;
	}

	//*****
	// Write a bool into the registry key. Return true if successful
	inline bool RegistryKey::Write(const char* key, bool value)
	{
		if( !m_valid ) return false;
		DWORD b = (value) ? (1) : (0);
		return RegSetValueEx(m_hkey, key, 0, REG_DWORD, (const unsigned char*)&b, sizeof(DWORD)) == ERROR_SUCCESS;
	}

	//*****
	// Write a DWORD into the registry key. Return true if successful
	inline bool RegistryKey::Write(const char* key, DWORD value)
	{
		if( !m_valid ) return false;
		return RegSetValueEx(m_hkey, key, 0, REG_DWORD, (const unsigned char*)&value, sizeof(DWORD)) == ERROR_SUCCESS;
	}

	//*****
	// Write a float into the registry key. Return true if successful
	inline bool RegistryKey::Write(const char* key, float value)
	{
		if( !m_valid ) return false;
		char str[256];
		sprintf(str, "%f", value);
		DWORD length = (DWORD)(strlen(str) + 1);
		return RegSetValueEx(m_hkey, key, 0, REG_SZ, (const unsigned char*)str, length) == ERROR_SUCCESS;
	}

	//*****
	// Write a string into the registry key. Return true if successful
	inline bool RegistryKey::Write(const char* key, const char* string)
	{
		if( !m_valid ) return false;
		DWORD length = (DWORD)(strlen(string) + 1);
		return RegSetValueEx(m_hkey, key, 0, REG_SZ, (const unsigned char*)string, length) == ERROR_SUCCESS;
	}

	//*****
	// Write some raw data into the registry key. Return true if successful
	inline bool RegistryKey::Write(const char* key, const void* data, DWORD length)
	{
		if( !m_valid ) return false;
		return RegSetValueEx(m_hkey, key, 0, REG_BINARY, (const unsigned char*)data, length) == ERROR_SUCCESS;
	}

	//*****
	// Returns the length of a registry value in bytes. If the value type is a string,
	// this method returns the number of characters it contains (including the terminating
	// null character). Returns 0 if the value doesn't exist.
	inline DWORD RegistryKey::GetKeyLength(const char* key)
	{
		if( !m_valid ) return 0;

		DWORD type, length = 0;
		if( RegQueryValueEx(m_hkey, key, NULL, &type, NULL, &length) != ERROR_SUCCESS ) return 0;
   		return length;
	}
}//namespace pr

#endif//PR_REGISTRY_KEY_H
