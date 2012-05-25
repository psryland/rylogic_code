//************************************************************
//
//	Drawlist
//
//************************************************************

#ifndef PR_RDR_DRAWLIST_H
#define PR_RDR_DRAWLIST_H

#include "PR/Common/StdList.h"
#include "PR/Common/StdMap.h"
#include "PR/Common/PRObjectPool.h"
#include "PR/Renderer/DrawListElement.h"
#include "PR/Renderer/Instance.h"
#include "PR/Renderer/SortKey.h"

namespace pr
{
	namespace rdr
	{
        class Drawlist
		{
		private:
			typedef std::map<SortKey, DrawListElement*>				TSorter;
			typedef pr::ObjectPool<DrawListElement, 1000>			TDLEPool;
            typedef std::map<const InstanceBase*, DrawListElement*>	TInstanceToDLE;

		public:
			Drawlist();

			void	Clear();
			void	AddInstance   (const InstanceBase& instance);
			void	RemoveInstance(const InstanceBase& instance);

			const DrawListElement* Begin() const	{ return m_drawlist_end.m_drawlist_next; }
			const DrawListElement* End() const		{ return &m_drawlist_end; }
		
		private:
			DrawListElement& GetDLE();
			void ReturnDLEList(DrawListElement* element);

		private:
			DrawListElement	m_drawlist_end;

			TDLEPool		m_drawlist_element_pool;
			TInstanceToDLE	m_instance_to_dle;
			TSorter			m_sorter;
		};
	}//namespace rdr
}//namespace pr

#endif//PR_RDR_DRAWLIST_H
