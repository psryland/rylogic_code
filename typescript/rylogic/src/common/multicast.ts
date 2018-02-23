
/** A C#-isk event type */
export class MulticastDelegate<TSender, TArgs>
{
	private _handlers: Handler<TSender, TArgs>[];
	constructor()
	{
		this._handlers = [];
	}
	sub(handler: Handler<TSender, TArgs>)
	{
		let idx = this._handlers.indexOf(handler);
		if (idx == -1) this._handlers.push(handler);
	}
	unsub(handler: Handler<TSender, TArgs>)
	{
		let idx = this._handlers.indexOf(handler);
		if (idx != -1) this._handlers.splice(idx, 1);
	}
	invoke(sender: TSender, args: TArgs)
	{
		for (let i = 0; i != this._handlers.length; ++i)
			this._handlers[i](sender, args)
	}
}
export interface Handler<TSender, TArgs>
{
	(s: TSender, a: TArgs): void;
}