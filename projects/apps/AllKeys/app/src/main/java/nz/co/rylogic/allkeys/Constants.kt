package nz.co.rylogic.allkeys

// Running state of the app
enum class ERunMode
{
	Stopped,
	StepOne,
	Continuous,
}

// Visibility of the next chord
enum class ENextChordMode
{
	Hidden,
	OneBeatBefore,
	TwoBeatsBefore,
	BarBefore,
	Always,
}

// The instruments available in the sound font
enum class EInstruments(val value: Int)
{
	AcousticBass(0),
	ElectricBass(1),
	Piano(2),
	DrumKit(3),
}

// The instruments suitable for playing root notes
enum class ERootNoteInstruments(val value: Int)
{
	AcousticBass(EInstruments.AcousticBass.value),
	ElectricBass(EInstruments.ElectricBass.value),
	Piano(EInstruments.Piano.value),
}

// The keys corresponding to metronome sounds
enum class EMetronomeSounds(val value: Short)
{
	WoodblockHigh(75),
	WoodblockLow(76),
	WoodblockMed(77),
	CowBell(56),
	Tambourine(54),
	RimShot(37),
}

// App themes
enum class EThemes
{
	System,
	Light,
	Dark,
}