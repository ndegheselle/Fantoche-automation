namespace Fantoche.Worker.UserInterfaces
{
    /// <summary>A file as the window hands it to the page, see <see cref="PageFiles"/>.</summary>
    public sealed record PageResponse(int Status, string Reason, Stream? Content, string Headers);

    /// <summary>
    /// The files of the page of a user interface task, as the window of the application serves them :
    /// only the ones of its folder (see <see cref="LocalWorker.GetPageFolderAsync"/>), under a host of
    /// its own. Anything else is refused : a page shown on every machine
    /// a run is answered from reaches nothing but what its task ships.
    /// </summary>
    public sealed class PageFiles
    {
        public const string Host = "page.fantoche";

        /// <summary>What the window opens.</summary>
        public static readonly Uri Index = new($"https://{Host}/index.html");

        /// <summary>
        /// Its own files, inline code and what it builds itself (data and blob urls) : nothing from
        /// the network, which the page has no business reaching.
        /// </summary>
        private const string ContentSecurityPolicy = "default-src 'self' 'unsafe-inline' data: blob:";

        private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            [".html"] = "text/html; charset=utf-8",
            [".js"] = "text/javascript; charset=utf-8",
            [".mjs"] = "text/javascript; charset=utf-8",
            [".css"] = "text/css; charset=utf-8",
            [".json"] = "application/json; charset=utf-8",
            [".svg"] = "image/svg+xml",
            [".png"] = "image/png",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".gif"] = "image/gif",
            [".webp"] = "image/webp",
            [".woff"] = "font/woff",
            [".woff2"] = "font/woff2",
            [".ttf"] = "font/ttf",
            [".wasm"] = "application/wasm",
        };

        private readonly string _folder;

        /// <param name="folder">The folder of the page, see <c>IUserInterfaceTask.PageFolder</c>.</param>
        public PageFiles(string folder)
        {
            _folder = Path.GetFullPath(folder);
        }

        /// <summary>What the page gets for [uri] : a file of its folder, or a refusal.</summary>
        public PageResponse Read(string uri)
        {
            if (!Uri.TryCreate(uri, UriKind.Absolute, out Uri? requested)
                || requested.Scheme != Uri.UriSchemeHttps
                || !string.Equals(requested.Host, Host, StringComparison.OrdinalIgnoreCase))
                return Refused(403, "Forbidden");

            string relative = Uri.UnescapeDataString(requested.AbsolutePath.TrimStart('/'));
            string path = Path.GetFullPath(Path.Combine(_folder, relative));
            // "../" leads nowhere out of the folder.
            if (!path.StartsWith(_folder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                return Refused(403, "Forbidden");
            if (!File.Exists(path))
                return Refused(404, "Not Found");

            string contentType = ContentTypes.GetValueOrDefault(Path.GetExtension(path), "application/octet-stream");
            // Read whole : the window reads the stream after the handler returned.
            MemoryStream content = new(File.ReadAllBytes(path));
            return new PageResponse(200, "OK", content, Headers(contentType));
        }

        private static PageResponse Refused(int status, string reason) => new(status, reason, null, Headers("text/plain"));

        private static string Headers(string contentType)
            => $"Content-Type: {contentType}\r\nContent-Security-Policy: {ContentSecurityPolicy}\r\nCache-Control: no-store";
    }
}
