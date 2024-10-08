﻿using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Sabine.Shared.Configuration;
using Sabine.Shared.Data;
using Sabine.Shared.Database;
using Sabine.Shared.L10N;
using Sabine.Shared.Network;
using Yggdrasil.Data;
using Yggdrasil.Extensions;
using Yggdrasil.Logging;
using Yggdrasil.Scripting;
using Yggdrasil.Util;

namespace Sabine.Shared
{
	/// <summary>
	/// Base class for servers.
	/// </summary>
	public abstract class Server
	{
		/// <summary>
		/// Returns a reference to all conf files.
		/// </summary>
		public ConfFiles Conf { get; private set; } = new ConfFiles();

		/// <summary>
		/// Returns a reference to the server's script loader.
		/// </summary>
		protected ScriptLoader ScriptLoader { get; private set; }

		/// <summary>
		/// Returns a reference to the server's string localizer manager.
		/// </summary>
		public MultiLocalizer Localization { get; } = new MultiLocalizer();

		/// <summary>
		/// Initializes server instance.
		/// </summary>
		public Server()
		{
			// Register additional encodings, didn't have a better place to put this.
			// Necessary because we need EUC-KR in Packet.
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		}

		/// <summary>
		/// Starts the server.
		/// </summary>
		/// <param name="args"></param>
		public abstract void Run(string[] args);

		/// <summary>
		/// Changes current directory to the project's root folder.
		/// </summary>
		protected void NavigateToRoot()
		{
			// First go to the assemblies directory and then back from
			// there until we find the root folder.
			var appDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			Directory.SetCurrentDirectory(appDirectory);

			var folderNames = new[] { "bin", "user", "system" };
			var tries = 5;

			for (var i = 0; i < tries; ++i)
			{
				if (folderNames.All(a => Directory.Exists(a)))
					return;

				Directory.SetCurrentDirectory("../");
			}

			throw new DirectoryNotFoundException($"Failed to navigate to root folder. (Missing: {string.Join(", ", folderNames)})");
		}

		/// <summary>
		/// Loads all configuration files.
		/// </summary>
		/// <returns></returns>
		public ConfFiles LoadConf()
		{
			Log.Info("Loading configuration...");

			this.Conf.Load();

			Game.Version = this.Conf.Version.PacketVersion;
			Log.Info("Using packet version {0}.", Game.Version);

			PacketTable.Load();

			return this.Conf;
		}

		/// <summary>
		/// Loads localization files and updates cultural settings.
		/// </summary>
		/// <returns></returns>
		protected void LoadLocalization(ConfFiles conf)
		{
			CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo(conf.Localization.Culture);
			CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo(conf.Localization.CultureUi);
			Thread.CurrentThread.CurrentCulture = CultureInfo.DefaultThreadCurrentCulture;
			Thread.CurrentThread.CurrentUICulture = CultureInfo.DefaultThreadCurrentUICulture;

			var serverLanguage = conf.Localization.Language;
			var relativeFolderPath = "localization";
			var systemFolderPath = Path.Combine("system", relativeFolderPath);
			var userFolderPath = Path.Combine("system", relativeFolderPath);

			Log.Info("Loading localization...");

			// Load everything from user first, then check system, without
			// overriding the ones loaded from user
			foreach (var filePath in Directory.EnumerateFiles(userFolderPath, "*.po", SearchOption.AllDirectories))
			{
				var languageName = Path.GetFileNameWithoutExtension(filePath);
				this.Localization.Load(languageName, filePath);

				Log.Info("  loaded {0}.", languageName);
			}

			foreach (var filePath in Directory.EnumerateFiles(systemFolderPath, "*.po", SearchOption.AllDirectories))
			{
				var languageName = Path.GetFileNameWithoutExtension(filePath);
				if (this.Localization.Contains(languageName))
					continue;

				this.Localization.Load(languageName, filePath);

				Log.Info("  loaded {0}.", languageName);
			}


			Log.Info("  setting default language to {0}.", serverLanguage);

			// Try to set the default localizer, and warn the user about
			// missing localizers if the selected server language isn't
			// US english.
			if (!this.Localization.Contains(serverLanguage))
			{
				if (serverLanguage != "en-US")
					Log.Warning("Localization file '{0}.po' not found.", serverLanguage);
			}
			else
			{
				this.Localization.SetDefault(serverLanguage);
			}

			Sabine.Shared.Util.Localization.SetLocalizer(this.Localization.GetDefault());
		}

		/// <summary>
		/// Initializes database connection.
		/// </summary>
		/// <param name="db"></param>
		/// <param name="conf"></param>
		protected void InitDatabase(Db db, ConfFiles conf)
		{
			Log.Info("Initializing database...");

			try
			{
				db.Init(conf.Database.Host, conf.Database.Port, conf.Database.Username, conf.Database.Password, conf.Database.Name);
			}
			catch (Exception ex)
			{
				Log.Error("Unable to open database connection. ({0})", ex.Message);
				ConsoleUtil.Exit(1);
			}
		}

		/// <summary>
		/// Loads data from JSON files.
		/// </summary>
		public void LoadData()
		{
			Log.Info("Loading data...");

			this.LoadDataFile(SabineData.Features, "features.txt");
			this.LoadDataFile(SabineData.ExpTables, "exp.txt");
			this.LoadDataFile(SabineData.Items, "items.txt");
			this.LoadDataFile(SabineData.ItemNames, "item_names.txt");
			this.LoadDataFile(SabineData.Jobs, "jobs.txt");
			this.LoadDataFile(SabineData.Monsters, "monsters.txt");
			this.LoadDataFile(SabineData.Maps, "maps.txt");

			var cacheFileName = "map_cache.dat";
			for (var i = 0; i <= Game.Version; ++i)
			{
				var fileName = $"map_cache_{i}.dat";
				var filePath = Path.Combine("system", "data", fileName);

				if (File.Exists(filePath))
					cacheFileName = fileName;
			}

			this.LoadDataFile(SabineData.MapCache, cacheFileName);
		}

		/// <summary>
		/// Loads files for the given database from the given file.
		/// </summary>
		/// <param name="db"></param>
		/// <param name="fileName"></param>
		private void LoadDataFile(IDatabase db, string fileName)
		{
			try
			{
				var systemPath = Path.Combine("system", "data", fileName);
				var userPath = Path.Combine("user", "data", fileName);

				if (!File.Exists(systemPath))
				{
					Log.Error("LoadDataFile: File '{0}' not found.", systemPath);
					ConsoleUtil.Exit(1);
					return;
				}

				db.Clear();
				db.LoadFile(systemPath);
				foreach (var ex in db.GetWarnings())
					Log.Warning(ex);

				if (File.Exists(userPath))
				{
					db.LoadFile(userPath);
					foreach (var ex in db.GetWarnings())
						Log.Warning(ex);
				}

				if (db.Count == 1)
					Log.Info("  done loading {0} entry from {1}.", db.Count, fileName);
				else
					Log.Info("  done loading {0} entries from {1}.", db.Count, fileName);
			}
			catch (DatabaseErrorException ex)
			{
				Log.Error(ex);
				ConsoleUtil.Exit(1);
			}
		}

		/// <summary>
		/// Loads all scripts from given list.
		/// </summary>
		/// <param name="scriptCategory"></param>
		/// <param name="conf"></param>
		public void LoadScripts(string scriptCategory, ConfFiles conf)
		{
			if (this.ScriptLoader != null)
			{
				Log.Error("The script loader has been created already.");
				return;
			}

			Log.Info("Loading scripts...");

			var listFilePath = Path.Combine("system", "scripts", "scripts_" + scriptCategory + ".txt");

			if (!File.Exists(listFilePath))
			{
				Log.Error("Script list not found: " + listFilePath);
				return;
			}

			var timer = Stopwatch.StartNew();

			try
			{
				var cachePath = (string)null;
				//if (this.Conf.Scripts.CacheScripts)
				//	cachePath = Path.Combine("user", "cache", "scripts", scriptCategory + ".dll");

				var userPath = Path.Combine("user", "scripts");
				var systemPath = Path.Combine("system", "scripts");

				this.ScriptLoader = new ScriptLoader(cachePath);
				this.ScriptLoader.LoadFromListFile(listFilePath, userPath, systemPath);

				foreach (var ex in this.ScriptLoader.ReferenceExceptions)
					Log.Warning(ex);

				foreach (var ex in this.ScriptLoader.LoadingExceptions)
				{
					if (ex.InnerException is MissingMethodException)
						Log.Error("It appears like a script tried to use a method that does (no longer) exist, which may be a caching issue. Try deleting the user/cache/ folder and run the server again.");

					Log.Error(ex);
				}
			}
			catch (CompilerErrorException ex)
			{
				this.DisplayScriptErrors(ex);
			}
			catch (Exception ex)
			{
				Log.Error(ex);
			}

			Log.Info("  loaded {0} scripts from {3} files in {2:n2}s ({1} init fails).", this.ScriptLoader.LoadedCount, this.ScriptLoader.FailCount, timer.Elapsed.TotalSeconds, this.ScriptLoader.FileCount);
		}

		/// <summary>
		/// Reloads previously loaded scripts.
		/// </summary>
		public void ReloadScripts()
		{
			Log.Info("Reloading scripts...");

			var timer = Stopwatch.StartNew();

			try
			{
				this.ScriptLoader.Reload();
			}
			catch (CompilerErrorException ex)
			{
				this.DisplayScriptErrors(ex);
			}
			catch (Exception ex)
			{
				Log.Error(ex);
			}

			Log.Info("  reloaded {0} scripts from {3} files in {2:n2}s ({1} init fails).", this.ScriptLoader.LoadedCount, this.ScriptLoader.FailCount, timer.Elapsed.TotalSeconds, this.ScriptLoader.FileCount);
		}

		/// <summary>
		/// Displays the script errors in the console.
		/// </summary>
		/// <param name="ex"></param>
		private void DisplayScriptErrors(CompilerErrorException ex)
		{
			foreach (var err in ex.Errors)
			{
				if (string.IsNullOrWhiteSpace(err.FileName))
				{
					Log.Error("While loading scripts: " + err.ErrorText);
				}
				else
				{
					var relativefileName = err.FileName;
					var cwd = Directory.GetCurrentDirectory();
					if (relativefileName.StartsWith(cwd, StringComparison.InvariantCultureIgnoreCase))
						relativefileName = relativefileName.Substring(cwd.Length + 1);

					var lines = File.ReadAllLines(err.FileName);
					var sb = new StringBuilder();

					// Error msg
					sb.AppendLine("In {0} on line {1}, column {2}", relativefileName, err.Line, err.Column);
					sb.AppendLine("          {0}", err.ErrorText);

					// Display lines around the error
					var startLine = Math.Max(1, err.Line - 1);
					var endLine = Math.Min(lines.Length, startLine + 2);
					for (var i = startLine; i <= endLine; ++i)
					{
						// Make sure we don't get out of range.
						// (ReadAllLines "trims" the input)
						var line = (i <= lines.Length) ? lines[i - 1] : "";

						sb.AppendLine("  {2} {0:0000}: {1}", i, line, (err.Line == i ? '*' : ' '));
					}

					if (err.IsWarning)
						Log.Warning(sb.ToString());
					else
						Log.Error(sb.ToString());
				}
			}
		}
	}
}
