using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Fantoche.Plugins.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fantoche.Worker.UserInterfaces
{
    /// <summary>A file as the window hands it to the page, see <see cref="PageFiles"/>.</summary>
    public sealed record PageResponse(int Status, string Reason, Stream? Content, string Headers);

    /// <summary>
    /// What the page of a user interface task reaches, as the window of the application serves it,
    /// under a host of its own :
    /// <list type="bullet">
    /// <item>the files of its folder (see <see cref="LocalWorker.GetPageFolderAsync"/>) ;</item>
    /// <item>under <see cref="StoragePath"/>, the files and tables of the storage of its run that its
    /// input points at : <c>files/{id}/{name}</c> as they are, <c>tables/{id}?offset=0&amp;limit=100</c>
    /// as a <see cref="TablePage"/> in JSON ;</item>
    /// <item>Pico (<see cref="PicoPath"/>), linked by every HTML file before its own styles : the pages
    /// look alike and follow the theme of the application without shipping a style of their own.</item>
    /// </list>
    /// Anything else is refused : a page shown on every machine a run is answered from reaches nothing
    /// but what its task ships and what it is handed.
    /// </summary>
    public sealed class PageFiles
    {
        public const string Host = "page.fantoche";

        /// <summary>What the window opens.</summary>
        public static readonly Uri Index = new($"https://{Host}/index.html");

        /// <summary>Where the storage of the run is served, out of the way of the files of the page.</summary>
        public const string StoragePath = "/.fantoche/";
        private const string FilesPath = $"{StoragePath}files/";
        private const string TablesPath = $"{StoragePath}tables/";
        public const string PicoPath = $"{StoragePath}pico.css";

        /// <summary>
        /// Inserted at the top of the head of every HTML file. Pico grows its font with the width of
        /// the window, made for a site rather than a dialog : the size is pinned, a page setting its
        /// own after.
        /// </summary>
        private const string PicoLink = $$"""<link rel="stylesheet" href="{{PicoPath}}"><style>:root { --pico-font-size: 87.5%; }</style>""";

        private static readonly Regex HeadTag = new(@"<head(\s[^>]*)?>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public const int DefaultRowsLimit = 100;
        /// <summary>Keeps a single response small : a page wanting more reads the next rows.</summary>
        public const int MaxRowsLimit = 1000;

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
            [".txt"] = "text/plain; charset=utf-8",
            [".csv"] = "text/csv; charset=utf-8",
            [".pdf"] = "application/pdf",
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
        private readonly IStorage _storage;
        /// <summary>Guarded by itself : a page method hands ids over while the page reads.</summary>
        private readonly HashSet<string> _reachableIds = [];

        /// <param name="folder">The folder of the page, see <c>IUserInterfaceTask.PageFolder</c>.</param>
        /// <param name="storage">The storage of the run of the task, wherever it is held.</param>
        /// <param name="input">What the page is asked with : the files and tables of [storage] it can read.</param>
        public PageFiles(string folder, IStorage storage, JToken? input)
        {
            _folder = Path.GetFullPath(folder);
            _storage = storage;
            Allow(input);
        }

        /// <summary>
        /// Let the page read the files and tables [value] holds, what a page method handed back.
        /// Any string of it, rather than the handles alone : whatever shape it has, an id is only
        /// reachable once handed over.
        /// </summary>
        public void Allow(JToken? value)
        {
            IEnumerable<JToken> tokens = value is JContainer container ? container.DescendantsAndSelf() : value == null ? [] : [value];
            IEnumerable<string> strings = tokens.Where(x => x.Type == JTokenType.String).Select(x => x.Value<string>()!);
            lock (_reachableIds)
                _reachableIds.UnionWith(strings);
        }

        private bool IsReachable(string id)
        {
            lock (_reachableIds)
                return _reachableIds.Contains(id);
        }

        /// <summary>What the page gets for [uri] : a file of its folder or of the storage, or a refusal.</summary>
        public async Task<PageResponse> ReadAsync(string uri, CancellationToken cancellation = default)
        {
            if (!Uri.TryCreate(uri, UriKind.Absolute, out Uri? requested)
                || requested.Scheme != Uri.UriSchemeHttps
                || !string.Equals(requested.Host, Host, StringComparison.OrdinalIgnoreCase))
                return Refused(403, "Forbidden");

            string path = requested.AbsolutePath;
            if (string.Equals(path, PicoPath, StringComparison.OrdinalIgnoreCase))
                return ReadPico();
            if (path.StartsWith(FilesPath, StringComparison.OrdinalIgnoreCase))
                return await ReadStoredFileAsync(path[FilesPath.Length..], cancellation);
            if (path.StartsWith(TablesPath, StringComparison.OrdinalIgnoreCase))
                return await ReadTableAsync(path[TablesPath.Length..], requested.Query, cancellation);
            return ReadPageFile(path);
        }

        private PageResponse ReadPageFile(string absolutePath)
        {
            string relative = Uri.UnescapeDataString(absolutePath.TrimStart('/'));
            string path = Path.GetFullPath(Path.Combine(_folder, relative));
            // "../" leads nowhere out of the folder.
            if (!path.StartsWith(_folder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                return Refused(403, "Forbidden");
            if (!File.Exists(path))
                return Refused(404, "Not Found");

            if (Path.GetExtension(path).Equals(".html", StringComparison.OrdinalIgnoreCase))
                return ReadHtml(path);

            // Read whole : the window reads the stream after the handler returned.
            MemoryStream content = new(File.ReadAllBytes(path));
            return new PageResponse(200, "OK", content, Headers(ContentTypeOf(path)));
        }

        /// <summary>[path] with Pico linked before anything of its own, so its styles win over Pico's.</summary>
        private static PageResponse ReadHtml(string path)
        {
            string html = File.ReadAllText(path);
            Match head = HeadTag.Match(html);
            string linked = head.Success
                ? html.Insert(head.Index + head.Length, PicoLink)
                : PicoLink + html;

            MemoryStream content = new(Encoding.UTF8.GetBytes(linked));
            return new PageResponse(200, "OK", content, Headers(ContentTypes[".html"]));
        }

        private static PageResponse ReadPico()
        {
            Stream content = typeof(PageFiles).Assembly.GetManifestResourceStream("Fantoche.Worker.UserInterfaces.pico.min.css")
                ?? throw new InvalidOperationException("Pico isn't embedded in the worker.");
            return new PageResponse(200, "OK", content, Headers(ContentTypes[".css"]));
        }

        /// <summary>[segments] being "{id}/{name}", as <see cref="FileRef"/> holds them.</summary>
        private async Task<PageResponse> ReadStoredFileAsync(string segments, CancellationToken cancellation)
        {
            string[] parts = segments.Split('/');
            if (parts.Length != 2)
                return Refused(404, "Not Found");

            FileRef file = new() { Id = Uri.UnescapeDataString(parts[0]), Name = Uri.UnescapeDataString(parts[1]) };
            if (!IsReachable(file.Id))
                return Refused(403, "Forbidden", $"The file [{file.Id}] was never handed to the page.");

            try
            {
                await using Stream stored = await _storage.OpenFileAsync(file, cancellation);
                MemoryStream content = new();
                await stored.CopyToAsync(content, cancellation);
                content.Position = 0;
                return new PageResponse(200, "OK", content, Headers(ContentTypeOf(file.Name)));
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                return NotInStorage($"The file [{file.Id}]", exception);
            }
        }

        /// <summary>[id] being the one of a <see cref="TableRef"/>, [query] "?offset=&amp;limit=".</summary>
        private async Task<PageResponse> ReadTableAsync(string id, string query, CancellationToken cancellation)
        {
            TableRef table = new() { Id = Uri.UnescapeDataString(id) };
            if (!IsReachable(table.Id))
                return Refused(403, "Forbidden", $"The table [{table.Id}] was never handed to the page.");

            var arguments = HttpUtility.ParseQueryString(query);
            long offset = long.TryParse(arguments["offset"], out long requestedOffset) ? Math.Max(0, requestedOffset) : 0;
            int limit = int.TryParse(arguments["limit"], out int requestedLimit) ? Math.Clamp(requestedLimit, 0, MaxRowsLimit) : DefaultRowsLimit;

            try
            {
                TablePage page = await _storage.ReadPageAsync(table, offset, limit, cancellation);
                string json = JsonConvert.SerializeObject(page);
                MemoryStream content = new(Encoding.UTF8.GetBytes(json));
                return new PageResponse(200, "OK", content, Headers(ContentTypes[".json"]));
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                return NotInStorage($"The table [{table.Id}]", exception);
            }
        }

        /// <summary>
        /// Unknown to the storage, or the storage out of reach : the supervisor holding a remote one
        /// gone, for instance.
        /// </summary>
        private static PageResponse NotInStorage(string item, Exception exception)
            => Refused(404, "Not Found", $"{item} can't be read from the storage of the run : {exception.Message}");

        private static string ContentTypeOf(string path)
            => ContentTypes.GetValueOrDefault(Path.GetExtension(path), "application/octet-stream");

        private static PageResponse Refused(int status, string reason, string? message = null)
        {
            Stream? content = message == null ? null : new MemoryStream(Encoding.UTF8.GetBytes(message));
            return new(status, reason, content, Headers("text/plain; charset=utf-8"));
        }

        private static string Headers(string contentType)
            => $"Content-Type: {contentType}\r\nContent-Security-Policy: {ContentSecurityPolicy}\r\nCache-Control: no-store";
    }
}
