using System.IO;
using System.Windows;
using Fantoche.Worker.Packages;
using Fantoche.Worker.Storage;
using Fantoche.Worker.UserInterfaces;
using Joufflu.Themes;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fantoche.App.Features.UserInterfaces
{
    /// <summary>
    /// The page of a user interface task, until the user answers it or closes it. It only reaches
    /// the files of its folder and the storage its input points at (see <see cref="PageFiles"/>), and talks to the application through
    /// <c>window.fantoche</c> alone : no other window, navigation nor object of the application.
    /// </summary>
    public partial class PageWindow : Window
    {
        /// <summary>
        /// What the page is handed : its input, and the answer as a promise rejected with the reason
        /// the answer was refused. Set before the page runs anything of its own.
        /// </summary>
        private const string ContractScript = """
            (() => {
                const waiting = [];
                window.chrome.webview.addEventListener('message', event => {
                    const reply = waiting.shift();
                    if (!reply)
                        return;
                    if (event.data.type === 'refused')
                        reply.reject(new Error(event.data.error));
                    else
                        reply.resolve();
                });
                window.fantoche = Object.freeze({
                    input: __INPUT__,
                    answer(value) {
                        return new Promise((resolve, reject) => {
                            waiting.push({ resolve, reject });
                            window.chrome.webview.postMessage({ type: 'answer', value });
                        });
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

        private readonly PageFiles _files;
        private readonly JToken? _input;
        private readonly string _userDataFolder;
        private readonly Func<JToken, Task<string?>> _submit;
        private readonly TaskCompletionSource<bool> _closed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private bool _isAnswered;
        private bool _isSubmitting;

        private PageWindow(string title, string folder, LocalStorage storage, JToken? input, string userDataFolder, Func<JToken, Task<string?>> submit)
        {
            InitializeComponent();
            Title = title;
            _files = new PageFiles(folder, storage, input);
            _input = input;
            _userDataFolder = userDataFolder;
            _submit = submit;

            Loaded += async (_, _) => await InitializeAsync();
            Closed += (_, _) => _closed.TrySetResult(_isAnswered);
        }

        /// <summary>
        /// Show the page of [folder] with [input] until it is answered (true) or closed (false).
        /// [submit] takes the answer, telling why it was refused, null once it is stored.
        /// </summary>
        /// <param name="storage">The storage of the run, whose files and tables [input] points at.</param>
        /// <param name="userDataFolder">Where the browser keeps its data, shared by the pages.</param>
        public static Task<bool> ShowAsync(string title, string folder, LocalStorage storage, JToken? input, string userDataFolder, Func<JToken, Task<string?>> submit)
        {
            PageWindow window = new(title, folder, storage, input, userDataFolder, submit)
            {
                Owner = Application.Current.MainWindow,
            };
            window.Show();
            return window._closed.Task;
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

            string input = _input?.ToString(Formatting.None) ?? "null";
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
            string? type = message["type"]?.Value<string>();
            if (type == "close")
            {
                Close();
                return;
            }

            if (type != "answer")
                return;

            // An answer at a time : a second click while the first is stored is refused.
            if (_isSubmitting || _isAnswered)
            {
                Reply(new JObject { ["type"] = "refused", ["error"] = "An answer is already being given." });
                return;
            }

            _isSubmitting = true;
            string? refusal = await _submit(message["value"] ?? JValue.CreateNull());
            _isSubmitting = false;

            if (refusal != null)
            {
                Reply(new JObject { ["type"] = "refused", ["error"] = refusal });
                return;
            }

            Reply(new JObject { ["type"] = "answered" });
            _isAnswered = true;
            Close();
        }

        private void Reply(JObject message) => View.CoreWebView2.PostWebMessageAsJson(message.ToString(Formatting.None));
    }
}
