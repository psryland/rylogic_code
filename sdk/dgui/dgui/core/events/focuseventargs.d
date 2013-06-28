module dgui.core.events.focuseventargs;

public import dgui.core.events.eventargs;

class FocusEventArgs: EventArgs
{
	private bool _focused;
	
	public this(bool fcs)
	{
		this._focused = fcs;
	}
	
	@property bool focused()
	{
		return this._focused;
	}
}