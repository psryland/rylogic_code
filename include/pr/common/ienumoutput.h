//********************************************************
//
//	Output Iterator
//
//********************************************************
// Use this to make an interface for receiving 'OutputType's
// and adding them to an output iterator

#ifndef PR_OUTPUT_ITERATOR_H
#define PR_OUTPUT_ITERATOR_H

namespace pr
{
	template <typename OutputType>
	struct IEnumOutput
	{
		virtual bool Add(const OutputType& out) = 0;
	};

	template <typename OutIter, typename OutputType>
	struct OutIterHelper : IEnumOutput<OutputType>
	{
		OutIterHelper(OutIter out_iter) : m_out_iter(out_iter) {}
		bool Add(const OutputType& out) { *m_out_iter++ = out; return true; }
		OutIter m_out_iter;
	};
}//namespace pr

#endif//PR_OUTPUT_ITERATOR_H