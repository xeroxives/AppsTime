using System;
using System.Collections.Generic;

namespace AppsTime.Models
{
	public class CustomData
	{
		public string Version { get; set; } = "1.0";
		public string TimeFormat { get; set; } = "hh_mm_ss";
		public DateTime LastModified { get; set; } = DateTime.Now;

		public string Language { get; set; } = "ru";
		public bool AutoStart { get; set; } = false;
		public bool MinimizeOnStart { get; set; } = true;
		public bool MinimizeOnExit { get; set; } = true;

		public Dictionary<string, string> NameAliases { get; set; } = new();
		public Dictionary<string, int> TimeOverrides { get; set; } = new();
		public HashSet<string> ExcludedProcesses { get; set; } = new();
		public List<string> PinnedProcesses { get; set; } = new();
	}
}