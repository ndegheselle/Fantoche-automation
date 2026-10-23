using System.Net;
using System.Net.Http.Json;
using Fantoche.Shared.Remote;

namespace Fantoche.Services.Remote;

/// <summary>
/// The HTTP API of the supervisor : the data written the way the supervisor reads it, and its
/// failures turned back into the exceptions the services throw (see <see cref="SupervisorErrors"/>).
/// </summary>
internal sealed class SupervisorHttp
{
    private sealed class Problem
    {
        public string? Type { get; set; }
        public string? Detail { get; set; }
        public Dictionary<string, List<string>>? Errors { get; set; }
    }

    private readonly HttpClient _client;

    /// <param name="client">Its base address being the supervisor, ending with a "/".</param>
    public SupervisorHttp(HttpClient client)
    {
        _client = client;
    }

    public async Task<T> GetAsync<T>(string url)
    {
        using HttpResponseMessage response = await _client.GetAsync(url);
        return await ReadAsync<T>(response);
    }

    /// <summary>Null when the supervisor knows nothing under [url].</summary>
    public async Task<T?> FindAsync<T>(string url) where T : class
    {
        using HttpResponseMessage response = await _client.GetAsync(url);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        return await ReadAsync<T>(response);
    }

    /// <summary>
    /// [body] is written as a <typeparamref name="TBody"/> : an element is sent as the base type
    /// the supervisor reads, with the name of its concrete type.
    /// </summary>
    public async Task<TResult> SendAsync<TBody, TResult>(HttpMethod method, string url, TBody body)
    {
        using HttpRequestMessage request = new(method, url) { Content = JsonContent.Create(body, options: FantocheJson.Options) };
        using HttpResponseMessage response = await _client.SendAsync(request);
        return await ReadAsync<TResult>(response);
    }

    public async Task SendAsync<TBody>(HttpMethod method, string url, TBody body)
    {
        using HttpRequestMessage request = new(method, url) { Content = JsonContent.Create(body, options: FantocheJson.Options) };
        using HttpResponseMessage response = await _client.SendAsync(request);
        await EnsureSuccessAsync(response);
    }

    public async Task<T> DeleteAsync<T>(string url)
    {
        using HttpResponseMessage response = await _client.DeleteAsync(url);
        return await ReadAsync<T>(response);
    }

    public async Task DeleteAsync(string url)
    {
        using HttpResponseMessage response = await _client.DeleteAsync(url);
        await EnsureSuccessAsync(response);
    }

    public async Task<T> PostFileAsync<T>(string url, string filePath)
    {
        await using FileStream stream = File.OpenRead(filePath);
        using MultipartFormDataContent content = new() { { new StreamContent(stream), "file", Path.GetFileName(filePath) } };
        using HttpResponseMessage response = await _client.PostAsync(url, content);
        return await ReadAsync<T>(response);
    }

    public async Task PutStreamAsync(string url, Stream content)
    {
        using StreamContent body = new(content);
        using HttpResponseMessage response = await _client.PutAsync(url, body);
        await EnsureSuccessAsync(response);
    }

    /// <summary>
    /// The content of [url], read as it is downloaded : disposing the stream releases the response.
    /// </summary>
    public async Task<Stream> OpenStreamAsync(string url)
    {
        HttpResponseMessage response = await _client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        try
        {
            await EnsureSuccessAsync(response);
        }
        catch
        {
            response.Dispose();
            throw;
        }
        return await response.Content.ReadAsStreamAsync();
    }

    /// <summary>[value] escaped to be a parameter of an url.</summary>
    public static string Query(object? value) => Uri.EscapeDataString(value?.ToString() ?? "");

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        await EnsureSuccessAsync(response);
        T? value = await response.Content.ReadFromJsonAsync<T>(FantocheJson.Options);
        return value ?? throw new InvalidDataException($"The supervisor answered nothing to [{response.RequestMessage?.RequestUri}].");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        Problem? problem = null;
        try
        {
            problem = await response.Content.ReadFromJsonAsync<Problem>(FantocheJson.Options);
        }
        catch
        {
            // Not a problem the supervisor wrote (a proxy, a crash) : the status says enough.
        }

        string message = problem?.Detail ?? $"The supervisor answered {(int)response.StatusCode} {response.ReasonPhrase}.";
        throw SupervisorErrors.ToException(problem?.Type, message, problem?.Errors);
    }
}
