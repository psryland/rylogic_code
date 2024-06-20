package nz.co.rylogic.allkeys

enum class ERunMode
{
	Stopped,
	StepOne,
	Continuous,
}

enum class ENextChordMode
{
	Hidden,
	OneBeatBefore,
	TwoBeatsBefore,
	BarBefore,
	Always,
}

enum class EInstrument
{
	Piano,
	AcousticBass,
	ElectricBass,
}

enum class EMetronomeSounds
{
	Clave,
	WoodblockLow,
	WoodblockMed,
	WoodblockHigh,
	CowBell,
	Drumsticks,
}

enum class EThemes
{
	System,
	Light,
	Dark,
}