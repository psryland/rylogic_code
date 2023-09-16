using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

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
}
