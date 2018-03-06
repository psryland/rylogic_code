using System;
using Rylogic.Extn;

namespace EscapeVelocity
{
	public class GameInstance
	{
		public GameConstants Consts { get; private set; }
		public WorldState World { get; private set; }
		public Stockpile Stockpile { get; private set; }
		public ChemLab ChemLab { get; private set; }
		public ShipDesign ShipDesign { get; private set; }

		public GameInstance(int seed)
		{
			Consts      = new GameConstants(seed, true);
			World       = new WorldState(Consts);
			Stockpile   = new Stockpile();
			ChemLab     = new ChemLab(Consts);
			ShipDesign  = new ShipDesign(Consts, World);

			GenerateStartingMaterials();
		}

		public void Step(double elapsed)
		{
			World.Step(elapsed);
		}

		private void GenerateStartingMaterials()
		{
			{//hack
				//var rnd = new Random(0);

				// "Discover" some elements
				//var e1 = 1;//rnd.NextRange(1, Consts.ElementCount);
				//var e2 = 3;//rnd.NextRange(1, Consts.ElementCount);
				//var e3 = 9;//rnd.NextRange(1, Consts.ElementCount);
				//var e4 = 20;//rnd.NextRange(1, Consts.ElementCount);
				//var e5 = 10;//rnd.NextRange(1, Consts.ElementCount);
				//ChemLab.DiscoverElement(e1);
				//ChemLab.DiscoverElement(e2);
				//ChemLab.DiscoverElement(e3);
				for (int i = 1; i != Consts.ElementCount+1; ++i)
				{
					if (i == 26)
						ChemLab.DiscoverElement(i);
					else
						ChemLab.DiscoverElement(i);
				}
		
				// Discover some materials using these elements, plus some unknown elements
				for (int i = 1; i != Consts.ElementCount+1; ++i)
				for (int j = i; j != Consts.ElementCount+1; ++j)
				{
					if (i == 32 && j == 32)
						ChemLab.DiscoverCompound(i,j);
					else
						ChemLab.DiscoverCompound(i,j);
				}
				//ChemLab.DiscoverCompound(e4,e5);

				ChemLab.DumpElements();
				ChemLab.DumpCompounds();
			}
		}
	}
}