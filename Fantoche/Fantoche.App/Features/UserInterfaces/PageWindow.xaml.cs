using System.IO;
using System.Windows;
using Fantoche.Worker.Packages;
using Fantoche.Worker.UserInterfaces;
using Joufflu.Themes;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fantoche.App.Features.UserInterfaces
{
    /// <summary>
    /// The page of a task asking the user, until the user answers it or closes it. It only reaches
    /// the files of its folder and the storage its input points at (see <see cref="PageFiles"/>),
    /// and talks to the task through <c>window.fantoche</c> alone : no other window, navigation nor
    /// object of the application.
    /// </summary>
    public partial class PageWindow : Window
    {
        /// <summary>
        /// What the page is handed : its input, its answer and the page methods of the task as
        /// promises, rejected with the reason they were refused. Every request carries an id, which its
        /// reply comes back with : calls can be waited on at once. Set before the page runs anything
        /// of its own.
        /// </summary>
        private const string ContractScript = """
            (() => {
                const waiting = new Map();
                let lastId = 0;
                window.chrome.webview.addEventListener('message', event => {
                    const reply = waiting.get(event.data.id);
                    if (!reply)
                        return;
                    waiting.delete(event.data.id);
                    if (event.data.type === 'refused')
                        reply.reject(new Error(event.data.error));
                    else
                        reply.resolve(event.data.value);
                });
                function send(type, payload) {
                    return new Promise((resolve, reject) => {
                        const id = ++lastId;
                        waiting.set(id, { resolve, reject });
                        window.chrome.webview.postMessage({ id, type, ...payload });
                    });
                }
                window.fantoche = Object.freeze({
                    input: __INPUT__,
                    async answer(value) {
                        await send('answer', { value });
                    },
                    call(method, args) {
                        return send('call', { method, args: args === undefined ? null : args });
                    },
                    close() {
                        window.chrome.webview.postMessage({ type: 'close' });
                    },
                    fileUrl(file) {
                        return `__STORAGE__files/${encodeURIComponent(file.Id)}/${encodeURIComponent(file.Name)}`;
                    },
                    async readTable(table, { offset = 0, limit = __LIMIT__ } = {}) {
                        const response = await fetch(`__STORAGE__tables/${encodeURIComponent(table.Id)}?offset=${offset}&limit=${limit}`);
                        if (!response.ok)
                            throw new Error(await response.text());
                        return response.json();
                    },
                });
            })();
            """;

        private readonly PageRequest _request;
        private readonly PageFiles _files;
        private readonly string _userDataFolder;
        private readonly TaskCompletionSource _closed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        /// <summary>Stops the page methods still running once the window is closed.</summary>
        private readonly CancellationTokenSource _closing = new();
        private bool _isAnswered;

        /// <summary>Completed once the window is closed, answered or not.</summary>
        public Task Dismissed => _closed.Task;

        private PageWindow(PageRequest request, string userDataFolder)
        {
            InitializeComponent();
            Title = request.Instance.NodeName;
            _request = request;
            _files = new PageFiles(request.Folder, request.Storage, request.Input);
            _userDataFolder = userDataFolder;

            Loaded += async (_, _) => await InitializeAsync();
            Closed += (_, _) =>
            {
                _closing.Cancel();
                _closed.TrySetResult();
            };
        }

        /// <summary>
        /// Show the page of [request] until it is answered or closed, see <see cref="Dismissed"/>.
        /// </summary>
        /// <param name="userDataFolder">Where the browser keeps its data, shared by the pages.</param>
        public static PageWindow Open(PageRequest request, string userDataFolder)
        {
            PageWindow window = new(request, userDataFolder)
            {
                Owner = Application.Current.MainWindow,
            };
            window.Show();
            return window;
        }

        private async Task InitializeAsync()
        {
            try
            {
                Directory.CreateDirectory(_userDataFolder);
                CoreWebView2Environment environment = await CoreWebView2Environment.CreateAsync(null, _userDataFolder);
                await View.EnsureCoreWebView2Async(environment);
            }
            catch (Exception exception)
            {
                // Mostly the WebView2 runtime missing from the machine.
                _closed.TrySetException(new InvalidOperationException($"The page can't be shown : {exception.Message}", exception));
                Close();
                return;
            }

            CoreWebView2 browser = View.CoreWebView2;
            // The browser follows the theme of Windows, which the application may not : the page
            // would be light in a dark application.
            browser.Profile.PreferredColorScheme = ThemeManager.Instance.IsDark
                ? CoreWebView2PreferredColorScheme.Dark
                : CoreWebView2PreferredColorScheme.Light;
            browser.Settings.AreDevToolsEnabled = PackageCache.IsHostDebuggable();
            browser.Settings.AreHostObjectsAllowed = false;
            browser.Settings.AreDefaultContextMenusEnabled = false;
            browser.Settings.IsStatusBarEnabled = false;
            browser.Settings.IsWebMessageEnabled = true;

            browser.NewWindowRequested += (_, e) => e.Handled = true;
            browser.NavigationStarting += (_, e) =>
            {
                if (!e.Uri.StartsWith($"https://{PageFiles.Host}/", StringComparison.OrdinalIgnoreCase))
                    e.Cancel = true;
            };
            browser.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
            browser.WebResourceRequested += async (_, e) =>
            {
                // The storage is read asynchronously : the browser waits for the response until completed.
                CoreWebView2Deferral deferral = e.GetDeferral();
                try
                {
                    PageResponse response = await _files.ReadAsync(e.Request.Uri);
                    e.Response = browser.Environment.CreateWebResourceResponse(response.Content, response.Status, response.Reason, response.Headers);
                }
                finally
                {
                    deferral.Complete();
                }
            };
            browser.WebMessageReceived += async (_, e) => await OnMessageAsync(e.WebMessageAsJson);

            string input = _request.Input?.ToString(Formatting.None) ?? "null";
            string contract = ContractScript
                .Replace("__INPUT__", input)
                .Replace("__STORAGE__", PageFiles.StoragePath)
                .Replace("__LIMIT__", PageFiles.DefaultRowsLimit.ToString());
            await browser.AddScriptToExecuteOnDocumentCreatedAsync(contract);
            browser.Navigate(PageFiles.Index.ToString());
        }

        /// <summary>What the page posted through <c>window.fantoche</c>.</summary>
        private async Task OnMessageAsync(string json)
        {
            JToken message = JToken.Parse(json);
            JToken? id = message["id"];
            switch (message["type"]?.Value<string>())
            {
                case "close":
                    Close();
                    break;
                case "call":
                    await CallAsync(id, message["method"]?.Value<string>() ?? "", message["args"]);
                    break;
                case "answer":
                    Answer(id, message["value"] ?? JValue.CreateNull());
                    break;
            }
        }

        /// <summary>
        /// Call the page method [method] of the task : what it returns is handed to the page, which
        /// can read the files and tables it holds from then on.
        /// </summary>
        private async Task CallAsync(JToken? id, string method, JToken? args)
        {
            try
            {
                JToken? result = await _request.CallAsync(method, args, _closing.Token);
                _files.Allow(result);
                Reply(new JObject { ["id"] = id, ["type"] = "returned", ["value"] = result });
            }
            catch (Exception exception) when (!_closing.IsCancellationRequested)
            {
                Reply(new JObject { ["id"] = id, ["type"] = "refused", ["error"] = exception.Message });
            }
        }

        private void Answer(JToken? id, JToken value)
        {
            // An answer at a time : the task takes the first one, refusing the next.
            string? refusal = _isAnswered ? "The page was answered already." : _request.Answer(value);
            if (refusal != null)
            {
                Reply(new JObject { ["id"] = id, ["type"] = "refused", ["error"] = refusal });
                return;
            }

            _isAnswered = true;
            Reply(new JObject { ["id"] = id, ["type"] = "answered" });
            Close();
        }

        private void Reply(JObject message)
        {
            // Closed meanwhile, there is nobody left to reply to.
            if (_closing.IsCancellationRequested)
                return;
            View.CoreWebView2.PostWebMessageAsJson(message.ToString(Formatting.None));
        }
    }
}
