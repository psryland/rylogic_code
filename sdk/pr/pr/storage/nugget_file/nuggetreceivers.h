//******************************************************************
//
//	Nugget Receiver helpers
//
//******************************************************************
#ifndef PR_NUGGET_FILE_NUGGET_RECEIVERS_H
#define PR_NUGGET_FILE_NUGGET_RECEIVERS_H

#include "pr/storage/nugget_file/NuggetFileAssertEnable.h"
#include "pr/storage/nugget_file/Types.h"
#include "pr/storage/nugget_file/Interfaces.h"

namespace pr
{
	namespace nugget
	{
		//*****
		// A back inserter wrapper
		template <typename BackInserterType>
		struct BackInserter : INuggetReceiver
		{
			BackInserter(BackInserterType inserter) : m_inserter(inserter) {}
			Nugget* NewNugget() { return &*m_inserter++; }
			BackInserterType m_inserter;
		};

		//*****
		// A container inserter
		template <typename ContainerType>	
		struct Container : INuggetReceiver
		{
			Container(ContainerType& container) : m_container(&container) {}
			Nugget* NewNugget() { return (m_container->push_back(Nugget()), &m_container->back()); }
			ContainerType* m_container;
		};

		//*****
		// Simple struct for receiving one nugget
		struct SingleNuggetReceiver : INuggetReceiver
		{
			SingleNuggetReceiver(Nugget* nug) : m_nug(nug) {}
			Nugget* NewNugget() { return m_nug; }
			Nugget* m_nug;
		};

	}//namespace nugget
}//namespace pr

#endif//PR_NUGGET_FILE_NUGGET_RECEIVERS_H
