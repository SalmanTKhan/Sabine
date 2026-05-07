using System.IO;
using Sabine.Shared.Const;
using Sabine.Shared.Data.Databases;
using Xunit;

namespace Tests.Sabine.Shared.Era
{
	public class EraResolutionTests
	{
		[Fact]
		public void Empty_db_resolves_to_classic()
		{
			var db = new FeatureDb();
			Assert.Equal(global::Sabine.Shared.Const.Era.Classic, db.GetEra());
		}

		[Fact]
		public void PreRenewal_flag_resolves_to_pre_renewal()
		{
			var db = LoadDb("[{ name: \"PreRenewal\", enabled: true }]");
			Assert.Equal(global::Sabine.Shared.Const.Era.PreRenewal, db.GetEra());
		}

		[Fact]
		public void Renewal_flag_resolves_to_renewal()
		{
			var db = LoadDb("[{ name: \"Renewal\", enabled: true }]");
			Assert.Equal(global::Sabine.Shared.Const.Era.Renewal, db.GetEra());
		}

		[Fact]
		public void Renewal_takes_precedence_over_pre_renewal()
		{
			var db = LoadDb("[{ name: \"PreRenewal\", enabled: true }, { name: \"Renewal\", enabled: true }]");
			Assert.Equal(global::Sabine.Shared.Const.Era.Renewal, db.GetEra());
		}

		[Fact]
		public void Disabled_flags_fall_through_to_classic()
		{
			var db = LoadDb("[{ name: \"PreRenewal\", enabled: false }, { name: \"Renewal\", enabled: false }]");
			Assert.Equal(global::Sabine.Shared.Const.Era.Classic, db.GetEra());
		}

		private static FeatureDb LoadDb(string json)
		{
			var path = Path.GetTempFileName();
			File.WriteAllText(path, json);
			try
			{
				var db = new FeatureDb();
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
