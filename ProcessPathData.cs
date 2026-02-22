using System.Collections.Generic;

namespace AppsTime.Models
{
	public class ProcessPathData
	{
		// 👇 Словарь: Имя процесса → Полный путь к exe
		public Dictionary<string, string> ProcessPaths { get; set; } = new Dictionary<string, string>();
	}
}