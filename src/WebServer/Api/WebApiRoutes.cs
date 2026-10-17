using EmbedIO;
using EmbedIO.Actions;
using Sabine.Web.Database;

namespace Sabine.Web.Api
{
	public static class WebApiRoutes
	{
		public static void Register(EmbedIO.WebServer server, WebDb database, Sabine.Shared.Configuration.Files.WebConf conf)
		{
			var account = new AccountApi(database, conf);
			var patches = new PatchApi(conf);

			server.WithAction("/api/account/check-username", HttpVerbs.Get, account.CheckUsername);
			server.WithAction("/api/account/create", HttpVerbs.Post, account.Create);
			server.WithAction("/api/account/password-reset/request", HttpVerbs.Post, account.RequestPasswordReset);
			server.WithAction("/api/account/password-reset/validate", HttpVerbs.Post, account.ValidatePasswordReset);
			server.WithAction("/api/account/password-reset/confirm", HttpVerbs.Post, account.ConfirmPasswordReset);
			server.WithAction("/api/patch/manifest", HttpVerbs.Get, patches.Manifest);
		}
	}
}
