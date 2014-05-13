using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace pr.container
{
	public enum ListChg
	{
		PreReset      ,
		Reset         ,
		ItemPreAdd    ,
		ItemAdded     ,
		ItemPreRemove ,
		ItemRemoved   ,
		ItemChanged   ,
		Reordered     ,
		
		//ItemAddedOrRemoved = ItemAdded|ItemRemoved,
		//OrderChanged = ItemAdded|ItemRemoved|Reordered|Reset,
	}
}
