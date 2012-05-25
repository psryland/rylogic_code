//*****************************************
//*****************************************
#include "test.h"
#include <algorithm>
#include "pr/storage/zip/zip.h"
#include "pr/maths/maths.h"
#include "pr/common/prtypes.h"
#include "pr/common/stdvector.h"
#include "pr/common/crc.h"

namespace TestZip
{
	using namespace pr;
			
	struct Pred
	{
		void operator () (uint8& val) { val = (uint8)IRand(m_rand, 0, 2); }
		IRandom m_rand;
	};

	void Run()
	{
		TBinaryData data, compressed, uncompressed;
		for( uint i = 0; i < 10000; i += 100 )
		{
			data.clear();
			data.resize(i);
			std::for_each(data.begin(), data.end(), Pred());

			zip::EResult result = zip::Compress(&data[0], (uint)data.size(), compressed, 4);
			zip::Decompress(&compressed[0], (uint)compressed.size(), uncompressed);

			PR_ASSERT(1, data.size() == uncompressed.size());
			PR_ASSERT(1, Crc(&data[0], (uint)data.size()) == Crc(&uncompressed[0], (uint)uncompressed.size()));
			printf("%d - %s - \tsrc: %d bytes \tdst: %d bytes \tRatio: %3.3f\n",
				i,
				(result == zip::EResult_Success) ? ("cmpd") : ("copy"),
				data.size(),
				compressed.size(),
				compressed.size() / (float)data.size());
		}
		_getch();
	}
}//namespace TestZip
