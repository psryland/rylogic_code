//*************************************************************
// Unit Test for PRString
//*************************************************************
#include "unittest++/1.3/src/unittest++.h"
#include "pr/common/base64.h"

SUITE(PRBase64)
{
	TEST(TestBase64)
	{
		using namespace pr::base64;
		unsigned char src[1024], dst[1024];
		unsigned int src_length, dst_length;
		unsigned int len, dlen;

		// zero length data
		len = EncodeSize(0);                          CHECK(len == 0);
		Encode("", 0, dst, dst_length);               CHECK(dst_length == 0);
		Decode(dst, dst_length, src, src_length);     CHECK(src_length == 0);

		// one input char
		len = EncodeSize(1);                          CHECK(len == 4);
		Encode("A", 1, dst, dst_length);              CHECK(dst_length == 4 && dst[0]=='Q' && dst[1]=='Q' && dst[2]=='=' && dst[3]=='=');
		Decode(dst, dst_length, src, src_length);     CHECK(src_length == 1 && src[0]=='A');

		// two chars
		len = EncodeSize(2);                          CHECK(len == 4);
		Encode("AB", 2, dst, dst_length);             CHECK(dst_length == 4 && dst[0]=='Q' && dst[1]=='U' && dst[2]=='I' && dst[3]=='=');
		Decode(dst, dst_length, src, src_length);     CHECK(src_length == 2 && src[0]=='A' && src[1]=='B');

		// three chars
		len = EncodeSize(3);                          CHECK(len == 4);
		Encode("ABC", 3, dst, dst_length);            CHECK(dst_length == 4 && dst[0]=='Q' && dst[1]=='U' && dst[2]=='J' && dst[3]=='D');
		Decode(dst, dst_length, src, src_length);     CHECK(src_length == 3 && src[0]=='A' && src[1]=='B' && src[2]=='C');

		// four chars
		len = EncodeSize(4);                          CHECK(len == 8);
		Encode("ABCD", 4, dst, dst_length);           CHECK(dst_length == 8 && dst[0]=='Q' && dst[1]=='U' && dst[2]=='J' && dst[3]=='D' && dst[4]=='R' && dst[5]=='A');
		Decode(dst, dst_length, src, src_length);     CHECK(src_length == 4 && src[0]=='A' && src[1]=='B' && src[2]=='C' && src[3]=='D');

		// All bytes from 0 to ff
		unsigned char sbuf[256]; for (int i = 0; i != 256; ++i) sbuf[i] = (unsigned char)i;
		unsigned char dbuf[] =	"AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIj"
								"JCUmJygpKissLS4vMDEyMzQ1Njc4OTo7PD0+P0BBQkNERUZH"
								"SElKS0xNTk9QUVJTVFVWV1hZWltcXV5fYGFiY2RlZmdoaWpr"
								"bG1ub3BxcnN0dXZ3eHl6e3x9fn+AgYKDhIWGh4iJiouMjY6P"
								"kJGSk5SVlpeYmZqbnJ2en6ChoqOkpaanqKmqq6ytrq+wsbKz"
								"tLW2t7i5uru8vb6/wMHCw8TFxsfIycrLzM3Oz9DR0tPU1dbX"
								"2Nna29zd3t/g4eLj5OXm5+jp6uvs7e7v8PHy8/T19vf4+fr7"
								"/P3+/w==";
		unsigned int ssize = sizeof(sbuf), dsize = sizeof(dbuf) - 1;

		len = EncodeSize(ssize);
		CHECK(len == dsize);
		
		Encode(sbuf, ssize, dst, dst_length);
		CHECK(dst_length == dsize);
		for (size_t i = 0; i != dsize; ++i)
			CHECK(dst[i] == dbuf[i]);
		
		dlen = DecodeSize(len);
		CHECK(dlen < sizeof(src));
		Decode(dst, dst_length, src, src_length);
		CHECK(src_length == ssize);
		for (size_t i = 0; i != ssize; ++i)
			CHECK(src[i] == sbuf[i]);

		// Random binary data
		for (size_t i = 0; i != ssize; ++i)
			sbuf[i] = (unsigned char)(rand() & 0xFF);

		Encode(sbuf, ssize, dst, dst_length);
		Decode(dst, dst_length, src, src_length);
		CHECK(src_length == ssize);
		for (size_t i = 0; i != ssize; ++i)
			CHECK(src[i] == sbuf[i]);
	}
}
