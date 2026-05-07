using System.IO;
using Sabine.Shared.Data.Databases;

namespace Tests.Sabine.Shared.Refine
{
	internal static class TestHelpers
	{
		public static RefineDb LoadRefineDb(string json)
		{
			var path = Path.GetTempFileName();
			File.WriteAllText(path, json);
			try
			{
				var db = new RefineDb();
				db.LoadFile(path);
				return db;
			}
			finally
			{
				File.Delete(path);
			}
		}
	}
}
