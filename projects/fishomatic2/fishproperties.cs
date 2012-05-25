using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Xml.Serialization;

namespace Fishomatic2
{
	public class FishProperties
	{
		[Description("Fishomatic window always on top")]
		public bool			AlwaysOnTop			{get;set;}

		[Description("The key to press to cast")]
		public char			CastKey				{get;set;}

		[Description("How far the bobber moves before clicking to catch fish")]
		public int			MoveThreshold		{get;set;}
	
		[Description("The screen space area in which to search for the bobber")]
		public Rectangle	SearchArea			{get;set;}

		[Description("The size of the area to search while tracking the bobber")]
		public Size			SmallSearchSize		{get;set;}

		[Description("The colour to search for")]
		public Color		TargetColour		{get;set;}
		
		[Description("Tolerance when searching for the target colour")]
		public int			ColourTolerence		{get;set;}

		[Description("Length of time to wait between detecting the bobber's moved and clicking (ms)")]
		public int			ClickDelay			{get;set;}

		[Description("Length of time to wait after casting before looking for the bobber (ms)")]
		public int			AfterCastWait		{get;set;}

		[Description("Length of time to wait after catch a fish before casting again")]
		public int			AfterCatchWait		{get;set;}

		[Description("The maximum length of time the fishing process can take")]
		public int			MaxFishCycle		{get;set;}

		[Description("The length of time to wait before deciding the bobber can't be found")]
		public int			AbortTime			{get;set;}

		[Description("The key to press to apply baubles to a fishing pole")]
		public char			BaublesKey			{get;set;}

		[Description("The key to press to select your fishing pole when applying baubles")]
		public char			FishingPoleKey		{get;set;}

		[Description("The time to wait (in minutes) between reapplication of baubles")]
		public int			BaublesTime			{get;set;}

		[Description("The time to wait while baubles are being applied")]
		public int			BaublesApplyWait	{get;set;}

		// Set defaults
		public FishProperties()
		{
			AlwaysOnTop			= true;
			CastKey				= '7';
			MoveThreshold		= 9;
			SearchArea			= new Rectangle(90, 90, 990, 740);
			SmallSearchSize		= new Size(50,50);
			TargetColour		= Color.FromArgb(0x00962C1E);
			ColourTolerence		= 50;
			ClickDelay			= 250;
			AfterCastWait		= 3000;
			AfterCatchWait		= 3000;
			MaxFishCycle		= 17000;
			AbortTime			= 8000;
			BaublesKey			= '9';
			FishingPoleKey		= '0';
			BaublesTime			= 11;
			BaublesApplyWait	= 6000;
		}

		// Return a rect for the small search area, centred on 'position'
		public Rectangle SmallSearchArea(Point position)
		{
			return Rectangle.FromLTRB(
				position.X - SmallSearchSize.Width/2,
				position.Y - SmallSearchSize.Height/2,
				position.X + SmallSearchSize.Width/2,
				position.Y + SmallSearchSize.Height/2);
		}

		// Load/Save
		public void Save(string filepath)
		{
			using (StreamWriter sw = new StreamWriter(filepath))
				new XmlSerializer(typeof(FishProperties)).Serialize(sw, this);
		}
		public static FishProperties Load(string filepath)
		{
			try
			{
				using (StreamReader sr = new StreamReader(filepath))
					return (FishProperties)new XmlSerializer(typeof(FishProperties)).Deserialize(sr);
			}
			catch (IOException)
			{
				return new FishProperties();
			}
		}
	}
}
