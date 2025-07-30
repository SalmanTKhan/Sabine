using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Files;
using EmbedIO.Net;
using EmbedIO.WebApi;
using Sabine.Shared;
using Sabine.Web.Logging;
using Yggdrasil.Logging;
using Yggdrasil.Network.Communication;
using Yggdrasil.Network.TCP;
using Yggdrasil.Util;
using Yggdrasil.Util.Commands;

namespace Sabine.Web
{
	public class WebServer : Server
	{
		public readonly static WebServer Instance = new();

		private EmbedIO.WebServer _webServer;

		/// <summary>
		/// Runs the server.
		/// </summary>
		/// <param name="args"></param>
		public override void Run(string[] args)
		{
			ConsoleUtil.WriteHeader(nameof(Sabine), "Web", ConsoleColor.DarkYellow, ConsoleHeader.Title, ConsoleHeader.Subtitle);
			ConsoleUtil.LoadingTitle();

			this.NavigateToRoot();
			this.LoadConf();
			this.LoadLocalization(this.Conf);

			StartWebServer();

			ConsoleUtil.RunningTitle();
			new ConsoleCommands().Wait();
		}

		/// <summary>
		/// Starts web server.
		/// </summary>
		private void StartWebServer()
		{
			foreach (var port in this.Conf.Web.BindPorts)
			{
				try
				{
					var url = string.Format("http://*:{0}/", port);

					Swan.Logging.Logger.NoLogging();
					Swan.Logging.Logger.RegisterLogger(new YggdrasilLogger(LogLevel.Debug));

					EndPointManager.UseIpv6 = false;

					_webServer = new EmbedIO.WebServer(url);

					// The PHP module handles all requests to PHP scripts,
					// including defaulting to index.php and prioritizing
					// the user folder. Should this fail, we'll try static
					// requests to user and system.
					// TODO: Look into handling PHP scripts from a FileModule,
					//   adding a pre-processor.

					if (Directory.Exists("user/web/"))
					{
						_webServer.WithStaticFolder("/", "user/web/", false, fm =>
						{
							fm.DefaultDocument = "index.htm";
							fm.OnMappingFailed = FileRequestHandler.PassThrough;
							fm.OnDirectoryNotListable = FileRequestHandler.PassThrough;
						});
					}

					if (Directory.Exists("system/web/"))
					{
						_webServer.WithStaticFolder("/", "system/web/", false, fm =>
						{
							fm.DefaultDocument = "index.htm";
						});
					}

					_webServer.RunAsync();

					if (_webServer.State == WebServerState.Stopped)
					{
						Log.Error("Failed to start web server, make sure there's only one instance running.");
						ConsoleUtil.Exit(1);
					}

					// Disabled for now. Giving the URLs makes things easier,
					// but the IP should ideally match what the user will
					// actually use to connect.
					//Log.Info("Client XML Config:");
					//Log.Info("  ServerListURL: {0}", url + "toslive/patch/serverlist.xml");
					//Log.Info("  StaticConfigURL: {0}", url + "toslive/patch/");

					Log.Status("Server now running on '{0}'", url);
				}
				catch (Exception ex)
				{
					Log.Error("Failed to start web server: {0}", ex);
					ConsoleUtil.Exit(1);
				}
			}
		}
	}
}
