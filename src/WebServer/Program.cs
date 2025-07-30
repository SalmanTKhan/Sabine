using System;
using EmbedIO;
using Yggdrasil.Logging;
using Yggdrasil.Util;

namespace Sabine.Web
{
	class Program
	{
		static void Main(string[] args)
		{
			try
			{
				WebServer.Instance.Run(args);
			}
			catch (Exception ex)
			{
				Log.Error("Error on startup: {0}", ex);
				ConsoleUtil.Exit(1, true);
			}
		}
	}
}
