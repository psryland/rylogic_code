//*********************************************************
//	Cryptography functions
//*********************************************************
#include "pr/crypt/crypt.h"
#include "pr/common/assert.h"
#include "pr/filesys/fileex.h"
#include "pr/crypt/Md5.h"

namespace pr
{
	namespace crypt
	{
		static_assert(sizeof(MD5_CTX) == sizeof(MD5Context), "");

	}//namespace crypt
}//namespace pr

using namespace pr;

// MD5 functions
void crypt::MD5Begin(crypt::MD5Context& context)
{
	MD5Init(reinterpret_cast<MD5_CTX*>(&context));
}

//*****
// Add data
void crypt::MD5Add(crypt::MD5Context& context, const void* data, uint size)
{
	MD5Update(reinterpret_cast<MD5_CTX*>(&context), static_cast<const uint8*>(data), size);
}

//*****
// Add the contents of a file
void crypt::MD5AddFile(crypt::MD5Context& context, const char* filename)
{
	Handle file = pr::FileOpen(filename, GENERIC_READ, FILE_SHARE_READ, 0, OPEN_EXISTING, FILE_ATTRIBUTE_READONLY|FILE_FLAG_SEQUENTIAL_SCAN, 0);
	if (file == INVALID_HANDLE_VALUE) return;

	uint8 buffer[4096];
	DWORD bytes_read;
	while (pr::FileRead(file, buffer, sizeof(buffer), &bytes_read))
		MD5Update(reinterpret_cast<MD5_CTX*>(&context), buffer, (uint)bytes_read);	
}

//****
// End the MD5 calculation
crypt::MD5 crypt::MD5End(crypt::MD5Context& context)
{
	MD5_CTX* md5_context = reinterpret_cast<MD5_CTX*>(&context);
	MD5Final(md5_context);
	return reinterpret_cast<MD5&>(md5_context->digest);
}
