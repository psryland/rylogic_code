namespace Rylogic.Utility;

using System;

/// <summary>CRTP Hard type generator for GUID</summary>
public abstract class UniqueId<T> where T : UniqueId<T>
{
	private readonly Guid m_value;

	protected UniqueId(Guid value)
	{
		m_value = value;
	}

	public static T NewId()
	{
		return (T?)Activator.CreateInstance(typeof(T), [Guid.NewGuid()])!;
	}

	public static T Empty
	{
		get => (T?)Activator.CreateInstance(typeof(T), [Guid.Empty])!;
	}

	public static implicit operator Guid(UniqueId<T> id)
	{
		return id.m_value;
	}

	public static bool operator ==(UniqueId<T> left, UniqueId<T> right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(UniqueId<T> left, UniqueId<T> right)
	{
		return !(left == right);
	}

	/// <inheritdoc/>
	public override bool Equals(object? obj)
	{
		return obj is UniqueId<T> id && m_value.Equals(id.m_value);
	}

	/// <inheritdoc/>
	public override int GetHashCode()
	{
		return m_value.GetHashCode();
	}

	/// <inheritdoc/>
	public override string ToString()
	{
		return m_value.ToString();
	}

	/// <summary>Shorten a guid string (mainly for use in debug output)</summary>
	public string Brief => ToString().Substring(8);
}
