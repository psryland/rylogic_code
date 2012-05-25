//****************************************************************************
//****************************************************************************
#include "test.h"
#include "pr/storage/nugget_file/nuggetfile.h"

using namespace pr;
using namespace pr::nugget;

namespace TestNuggetFile
{
	const char SomeData0[] = "Some Data0";
	const char SomeData1[] = "Some Data1";
	const char SomeData2[] = "Some Data2";
	const char SomeData3[] = "Some Data3";
	const char SomeData4[] = "Some Data4";
	const char Appendix[] = " Appendix";
	const char TempFileTestData[] = "Temp File Test Data";

	struct Source : ISrc
	{
		Source()
		{
			m_data.resize(sizeof(Header) + 10);
			Header h = Header::Construct(('N') | ('G' << 8) | ('T' << 16) | (1 << 24), 2, 1, 0, SomeData2);
			h.m_user_flags = 0;
			h.m_data_start = sizeof(Header);
			h.m_data_length = 10;

			memcpy(&m_data[0], &h, sizeof(h));
			memset(&m_data[sizeof(h)], 2, 10);
		}
		std::size_t Read(void* dest, std::size_t size, std::size_t offset) const
		{
			size = (m_data.size() - offset > size) ? (size) : (m_data.size() - offset);
			memcpy(dest, &m_data[offset], size);
			return size;
		}
		const void* GetData(std::size_t offset) const
		{
			return &m_data[offset];
		}
		std::size_t GetDataSize() const
		{
			return m_data.size();
		}
		TBinaryData m_data;
	};
	struct Destination : IDest
	{
		std::size_t Write(const void* src, std::size_t size, std::size_t offset)
		{
			if( offset + size > m_data.size() ) m_data.resize(offset + size);
			memcpy(&m_data[offset], src, size);
			return size;
		}
		TBinaryData m_data;
	};

	typedef std::vector<Nugget> TNuggetVec;

	void Run()
	{
		//Nugget();
		TNuggetVec nugget_vec(1);

		//void		Initialise(std::size_t id, std::size_t version, std::size_t flags, const char* description);
		//EResult	SetData(const void* data, std::size_t data_size, std::size_t copy_flag);
		nugget_vec[0].Initialise(0, 1, 0, SomeData0);
		Verify(nugget_vec[0].SetData(SomeData0, sizeof(SomeData0), ECopyFlag_Reference));

		//Nugget(std::size_t id, std::size_t version, const char* description);	// Construct empty nugget
		//Nugget(const Nugget<T>& copy);
		//EResult		SetData(const void* data, std::size_t data_size, std::size_t copy_flag);
		Nugget nug1(1, 1, 0, SomeData1);
		nug1.SetData(SomeData1, sizeof(SomeData1), ECopyFlag_CopyToBuffer);
		nugget_vec.push_back(nug1);

		//Nugget(ISrc& src, std::size_t offset, std::size_t copy_flag);		// Construct from nugget data
		//EResult		Initialise(ISrc& src, std::size_t offset, std::size_t copy_flag);
		//EResult		SetData(ISrc& src, std::size_t offset, std::size_t data_size, std::size_t copy_flag);
		//EResult		SetData(std::size_t id, std::size_t version, const char* description, ISrc& src, std::size_t offset, std::size_t data_size, std::size_t copy_flag);
		Source src;
		Nugget nug2(src, 0, ECopyFlag_Reference);
		nugget_vec.push_back(nug2);

		//Nugget(const void* data, std::size_t copy_flag);					// Construct from nugget data
		//EResult		Initialise(const void* data, std::size_t copy_flag);
		Nugget nug3(&src.m_data[0], ECopyFlag_CopyToTempFile);
		nugget_vec.push_back(nug3);

		//std::size_t		GetId() const					{ return m_header.m_id; }
		std::size_t id = nugget_vec[2].GetId(); id;

		//std::size_t		GetVersion() const				{ return m_header.m_version; }
		std::size_t ver = nugget_vec[1].GetVersion(); ver;

		//const char*	GetDescription() const			{ return m_header.m_description; }
		const char* desc = nugget_vec[3].GetDescription(); desc;

		//std::size_t SizeInBytes(const NuggetContainer& nuggets)
		//std::size_t		GetNuggetSizeInBytes() const	{ return m_header.m_data_start + m_header.m_data_length; }
		std::size_t total_size = SizeInBytes(nugget_vec.begin(), nugget_vec.end()); total_size;

		//std::size_t		GetDataSize() const				{ return m_header.m_data_length; }
		std::size_t nug_size = nugget_vec[0].GetDataSize(); nug_size;

		//EResult		BufferData(std::size_t copy_flag);
		//const void*	GetData() const;
		const void* data0 = nugget_vec[0].GetData(); data0;
		const void* data1 = nugget_vec[1].GetData(); data1;
		//const uint8* data2 = nugget_vec[2].GetData(); data2; //Source data=>Assert

		//EResult		GetData(IDest& dst, std::size_t offset) const;
		Destination dst;
		Verify(nugget_vec[3].GetData(dst, 0));

		//EResult		AppendData(const void* data, std::size_t data_size);
		nugget_vec[1].AppendData(Appendix, sizeof(Appendix), ECopyFlag_CopyToBuffer);
		
		//EResult		AppendData(ISrc& src, std::size_t offset, std::size_t data_size);
		Source src_append;
		nugget_vec[1].AppendData(src_append, 0, sizeof(src_append), ECopyFlag_CopyToBuffer);

		//EResult		Initialise(std::size_t id, std::size_t version, const char* description);
		nugget_vec[1].Initialise(4, 1, 0, SomeData4);

		// TempFile test
		Nugget nug5(5, 1, 0, "TempFileTest");
		nug5.SetData(TempFileTestData, sizeof(TempFileTestData), ECopyFlag_CopyToTempFile);
		nugget_vec.push_back(nug5);

		//EResult		Save(IDest& dst, std::size_t& offset) const;
		//EResult Save(IDest& dst, const NuggetContainer& nuggets)
		Destination save_dst;
		Save(save_dst, nugget_vec.begin(), nugget_vec.end());

		//EResult Load(ISrc& src, std::size_t src_size, std::size_t copy_flag, NuggetContainer& nuggets)
		TNuggetVec loaded_nuggets;
		nugget::Container<TNuggetVec> cntr(loaded_nuggets);
		Source load_src; load_src.m_data = save_dst.m_data;
		Load(load_src, (std::size_t)load_src.m_data.size(), ECopyFlag_CopyToBuffer, cntr);

		_getch();
	}
}