namespace UFADO.Utility;

using System;

public readonly struct ItemId
{
	private readonly long m_id;
	public ItemId(long id) => m_id = id;
	public static implicit operator long(ItemId id) => id.m_id;

	#region Equals
	public static bool operator ==(ItemId left, ItemId right) => left.m_id == right.m_id;
	public static bool operator !=(ItemId left, ItemId right) => left.m_id != right.m_id;
	public override bool Equals(object? obj) => obj is ItemId id && m_id == id.m_id;
	public override int GetHashCode() => HashCode.Combine(m_id);
	#endregion
}
