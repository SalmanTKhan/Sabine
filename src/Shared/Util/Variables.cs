using System;
using System.Collections.Generic;
using System.Linq;

namespace Sabine.Shared.Util
{
	/// <summary>
	/// Holds temporary and permanent variables.
	/// </summary>
	public class VariableContainer
	{
		/// <summary>
		/// Temporary variables, which aren't saved.
		/// </summary>
		public Variables Temp { get; } = new Variables();

		/// <summary>
		/// Permanent variables, which are saved to the database.
		/// </summary>
		public Variables Perm { get; } = new Variables();
	}

	/// <summary>
	/// Wrapper around a dictionary holding variables. Adds typed
	/// array operations on top of the Yggdrasil base — eAthena
	/// scripts use server/account/npc-scoped arrays heavily
	/// (`$@bingo[N]`, `$@textstrings$[N]`, etc.) and we want to
	/// model those as real <see cref="System.Int32"/> /
	/// <see cref="System.String"/> arrays rather than the eAthena
	/// strided-key encoding.
	/// </summary>
	/// <remarks>
	/// All methods read/write through the base class's
	/// <c>Get(name)</c> / <c>Set(name, value)</c> entry points, which
	/// take care of the internal synchronisation. The stored value
	/// is a plain <c>int[]</c> or <c>string[]</c>; the persistence
	/// layer in <c>Db.SaveVars</c> recognises these via the new
	/// <c>iA</c> / <c>sA</c> type codes.
	/// </remarks>
	public class Variables : Yggdrasil.Util.Variables
	{
		/// <summary>
		/// Returns the int array stored under <paramref name="name"/>,
		/// or an empty array when the key is missing or stored under
		/// a different type.
		/// </summary>
		public int[] GetIntArray(string name)
			=> this.Get(name) is int[] arr ? arr : Array.Empty<int>();

		/// <summary>
		/// Replaces the entire int-array value stored under
		/// <paramref name="name"/>. A copy of <paramref name="values"/>
		/// is taken so callers can mutate their source without
		/// affecting the stored snapshot.
		/// </summary>
		public void SetIntArray(string name, IEnumerable<int> values)
			=> this.Set(name, values?.ToArray() ?? Array.Empty<int>());

		/// <summary>
		/// Reads a single element from an int array. Returns
		/// <paramref name="defaultValue"/> when the key is missing,
		/// stored as a different type, or the index is out of range —
		/// matching eAthena's permissive array semantics.
		/// </summary>
		public int GetIntElement(string name, int index, int defaultValue = 0)
		{
			if (this.Get(name) is int[] arr && index >= 0 && index < arr.Length)
				return arr[index];
			return defaultValue;
		}

		/// <summary>
		/// Writes a single element into an int array, growing the
		/// backing array (and zero-filling intermediate slots) when
		/// the index is past the current end. eAthena's
		/// `setarray @arr[5], 9` shape is sparse-friendly; we
		/// mirror that here.
		/// </summary>
		public void SetIntElement(string name, int index, int value)
		{
			if (index < 0)
				return;

			var existing = this.Get(name) as int[] ?? Array.Empty<int>();
			if (index < existing.Length)
			{
				existing[index] = value;
				this.Set(name, existing);
				return;
			}

			var grown = new int[index + 1];
			Array.Copy(existing, grown, existing.Length);
			grown[index] = value;
			this.Set(name, grown);
		}

		/// <summary>
		/// String-array companion to <see cref="GetIntArray"/>.
		/// </summary>
		public string[] GetStringArray(string name)
			=> this.Get(name) is string[] arr ? arr : Array.Empty<string>();

		/// <summary>
		/// String-array companion to <see cref="SetIntArray"/>.
		/// </summary>
		public void SetStringArray(string name, IEnumerable<string> values)
			=> this.Set(name, values?.ToArray() ?? Array.Empty<string>());

		/// <summary>
		/// String-array companion to <see cref="GetIntElement"/>.
		/// Returns empty string for unset slots.
		/// </summary>
		public string GetStringElement(string name, int index, string defaultValue = "")
		{
			if (this.Get(name) is string[] arr && index >= 0 && index < arr.Length)
				return arr[index] ?? string.Empty;
			return defaultValue;
		}

		/// <summary>
		/// String-array companion to <see cref="SetIntElement"/>.
		/// </summary>
		public void SetStringElement(string name, int index, string value)
		{
			if (index < 0)
				return;

			var existing = this.Get(name) as string[] ?? Array.Empty<string>();
			if (index < existing.Length)
			{
				existing[index] = value ?? string.Empty;
				this.Set(name, existing);
				return;
			}

			var grown = new string[index + 1];
			Array.Copy(existing, grown, existing.Length);
			for (var i = existing.Length; i < grown.Length; i++)
				grown[i] ??= string.Empty;
			grown[index] = value ?? string.Empty;
			this.Set(name, grown);
		}

		/// <summary>
		/// eAthena `getarraysize`. Returns the element count of the
		/// array stored under <paramref name="name"/>, treating
		/// non-arrays and missing keys as zero-length.
		/// </summary>
		public int GetArrayLength(string name)
		{
			return this.Get(name) switch
			{
				int[] iarr => iarr.Length,
				string[] sarr => sarr.Length,
				_ => 0,
			};
		}

		/// <summary>
		/// eAthena `copyarray dst, src, count`. Copies up to
		/// <paramref name="count"/> elements from one stored array to
		/// another (within this same container). The destination
		/// array is replaced wholesale; existing entries past
		/// <paramref name="count"/> are dropped, matching rAthena's
		/// behaviour on a fresh-write copy.
		/// </summary>
		public void CopyArray(string dstName, string srcName, int count)
		{
			if (count <= 0)
				return;

			switch (this.Get(srcName))
			{
				case int[] iarr:
				{
					var len = Math.Min(count, iarr.Length);
					var copy = new int[len];
					Array.Copy(iarr, copy, len);
					this.Set(dstName, copy);
					break;
				}
				case string[] sarr:
				{
					var len = Math.Min(count, sarr.Length);
					var copy = new string[len];
					Array.Copy(sarr, copy, len);
					this.Set(dstName, copy);
					break;
				}
			}
		}
	}
}
