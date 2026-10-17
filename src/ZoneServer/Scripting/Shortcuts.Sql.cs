using System;
using System.Collections.Generic;
using Sabine.Zone;
using Yggdrasil.Logging;

namespace Sabine.Zone.Scripting
{
	public sealed class QuerySqlResult
	{
		private readonly List<object[]> rows;

		public QuerySqlResult(List<object[]> rows) => this.rows = rows ?? new();
		public int RowCount => this.rows.Count;

		public string[] GetStringColumn(int column)
		{
			var result = new string[this.rows.Count];
			for (var i = 0; i < result.Length; i++)
				result[i] = column < this.rows[i].Length && this.rows[i][column] != null
					? Convert.ToString(this.rows[i][column]) ?? string.Empty : string.Empty;
			return result;
		}

		public int[] GetIntColumn(int column)
		{
			var result = new int[this.rows.Count];
			for (var i = 0; i < result.Length; i++)
				result[i] = column < this.rows[i].Length && this.rows[i][column] != null
					? Convert.ToInt32(this.rows[i][column]) : 0;
			return result;
		}
	}

	public static partial class Shortcuts
	{
		public static QuerySqlResult QuerySqlRows(string sql)
		{
			var rows = new List<object[]>();
			if (string.IsNullOrWhiteSpace(sql))
				return new QuerySqlResult(rows);
			try
			{
				using var conn = ZoneServer.Instance.Database.GetConnection();
				using var cmd = new MySqlConnector.MySqlCommand(sql, conn);
				using var reader = cmd.ExecuteReader();
				while (reader.Read())
				{
					var row = new object[reader.FieldCount];
					for (var i = 0; i < row.Length; i++)
						row[i] = reader.IsDBNull(i) ? null : reader.GetValue(i);
					rows.Add(row);
				}
			}
			catch (Exception ex)
			{
				Log.Error("query_sql result failed: {0}", ex.Message);
			}
			return new QuerySqlResult(rows);
		}
	}
}
