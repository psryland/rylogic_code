using System;
using System.Collections.Generic;
using System.Diagnostics;
using cAlgo.API;
using cAlgo.API.Internals;

namespace Rylobot
{
    [Indicator(ScalePrecision = 5, AutoRescale = false, IsOverlay = true, AccessRights = AccessRights.FullAccess)]
    [Levels()]
    public class SessionsIndicator : Indicator
    {

        /// <summary>New York Session Open</summary>
        [Parameter("New York Open (GMT)", DefaultValue = "13:00")]
        public string NYOpen { get; set; }

        /// <summary>New York Session Close</summary>
        [Parameter("New York Close (GMT)", DefaultValue = "22:00")]
        public string MYClose { get; set; }

        /// <summary>New York Session region colour</summary>
        [Parameter("New York Colour", DefaultValue = Colors.LightSteelBlue)]
        public Colors NYColour { get; set; }

        //private ChartObjects m_regions;

        protected override void Initialize()
        {
            base.Initialize();
            //ChartObjects.DrawLine("r", 
        }
        public override void Calculate(int index)
        {
            //Debug.Write(index);
        }
    }
}
