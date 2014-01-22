#ifndef NANA_AUTO_BUF_HPP
#define NANA_AUTO_BUF_HPP
#include <memory>
namespace nana
{

	template<typename ElemType>
	class auto_buf
	{
		auto_buf(const auto_buf&);
		auto_buf& operator=(const auto_buf&);
	public:
		typedef ElemType value_type;

		auto_buf()
			:myptr_(0), size_(0)
		{}

		explicit auto_buf(std::size_t count)
			:myptr_(0), size_(0)
		{
			alloc(count);
		}

		~auto_buf()
		{
			if(size_ > 2)
				delete [] myptr_;
			else
				delete myptr_;

			myptr_ = 0;			
		}

		void alloc(std::size_t count)
		{
			if(size_ > 2)
				delete [] myptr_;
			else
				delete myptr_;

			myptr_ = 0;

			if(count >= 0)
			{
				size_ = count;
				if(count > 1)
					myptr_ = new value_type[size_];
				else
					myptr_ = new value_type;
			}
		}

		value_type* get() const
		{
			return myptr_;
		}

		value_type* release()
		{
			value_type* ret = myptr_;
			myptr_ = 0;
			size_ = 0;
			return ret;
		}

		std::size_t size() const
		{
			return size_;
		}
	private:
		value_type *	myptr_;
		size_t			size_;
	};

}//end namespace nana

#endif

