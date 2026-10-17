using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using EmbedIO;
using Sabine.Shared.Configuration.Files;

namespace Sabine.Web.Api
{
	public sealed class PatchApi
	{
		private readonly WebConf _conf;

		public PatchApi(WebConf conf)
		{
			_conf = conf;
		}

		public async Task Manifest(IHttpContext context)
		{
			var root = new XElement("patchlist",
				new XAttribute("format", "root-relative"),
				new XAttribute("custom_patch_start", "-1"),
				new XAttribute("cumulative_patch", "false"));

			foreach (var patchFolder in _conf.PatchFolders)
			{
				if (!Directory.Exists(patchFolder))
					continue;

				var folderRoot = Path.GetFullPath(patchFolder);
				foreach (var filePath in Directory.EnumerateFiles(folderRoot, "*", SearchOption.AllDirectories))
				{
					var relativePath = Path.GetRelativePath(folderRoot, filePath).Replace('\\', '/');
					if (!IsSafeRelativePath(relativePath))
						continue;

					var info = new FileInfo(filePath);
					root.Add(new XElement("patch",
						new XAttribute("path", relativePath),
						new XAttribute("url", "files/" + Uri.EscapeDataString(relativePath).Replace("%2F", "/")),
						new XAttribute("hash", Sha256(filePath)),
						new XAttribute("size", info.Length)));
				}
			}

			var document = new XDocument(root);
			context.Response.StatusCode = (int)HttpStatusCode.OK;
			context.Response.ContentType = "application/xml";
			await context.SendStringAsync(document.ToString(SaveOptions.DisableFormatting), "application/xml", Encoding.UTF8);
		}

		private static bool IsSafeRelativePath(string relativePath)
		{
			if (Path.IsPathRooted(relativePath) || relativePath.Contains(':'))
				return false;

			return !relativePath.Split('/').Any(part => part == "." || part == ".." || string.IsNullOrWhiteSpace(part));
		}

		private static string Sha256(string filePath)
		{
			using var stream = File.OpenRead(filePath);
			return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
		}
	}
}
