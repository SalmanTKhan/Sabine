using System;
using System.Collections;
using System.Collections.Generic;
using Yggdrasil.Util;

namespace Sabine.Zone.Scripting.Dialogues
{
	/// <summary>
	/// Represents an option in a dialog's menu.
	/// </summary>
	public class DialogOption
	{
		/// <summary>
		/// Gets or sets the text displayed in the menu.
		/// </summary>
		public string Text { get; set; }

		/// <summary>
		/// Gets or sets the key that is returned when this option
		/// is selected by the player.
		/// </summary>
		public string Key { get; set; }

		/// <summary>
		/// Gets or sets a function that determines whether a specific dialog option is enabled.
		/// </summary>
		public Func<bool> Enabled { get; set; }

		/// <summary>
		/// Creates new instance.
		/// </summary>
		/// <param name="text"></param>
		/// <param name="key"></param>
		public DialogOption(string text, string key)
		{
			this.Text = text;
			this.Key = key;
			this.Enabled = () => true; // Default to always enabled.
		}
	}

	/// <summary>
	/// A mutable list of dialog options.
	/// </summary>
	public class DialogOptionList : IEnumerable<DialogOption>
	{
		private readonly List<DialogOption> _options = new();

		public DialogOptionList(params DialogOption[] options)
		{
			_options.AddRange(options);
		}

		public void Add(DialogOption option) => _options.Add(option);

		public void Insert(int index, DialogOption option)
		{
			if (index < 0) index = _options.Count + index;
			index = Math.Clamp(index, 0, _options.Count);
			_options.Insert(index, option);
		}

		public void Remove(int index) => _options.RemoveAt(index);
		public void Remove(string optionKey) => _options.RemoveAll(a => a.Key == optionKey);

		public IEnumerator<DialogOption> GetEnumerator() => _options.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => _options.GetEnumerator();
	}
}
