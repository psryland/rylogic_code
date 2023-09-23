using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using UFADO.Utility;

namespace UFADO;

public static class Extensions
{
	/// <summary>Return a name for this work item</summary>
	public static string Title(this WorkItem wi)
	{
		if (wi.Fields.TryGetValue("System.Title", out var v)) return (string)v;
		if (wi.Fields.TryGetValue("System.Id", out v)) return $"#{v}";
		return "WorkItem";
	}

	/// <summary>Return this work item's parent</summary>
	public static ItemId? ItemId(this WorkItem wi)
	{
		if (wi.Fields.TryGetValue("System.Id", out var v)) return new((int)v);
		return null;
	}

	/// <summary>Return this work item's parent</summary>
	public static ItemId? ParentItemId(this WorkItem wi)
	{
		if (wi.Fields.TryGetValue("System.Parent", out var v)) return new((long)v);
		return null;
	}
}
