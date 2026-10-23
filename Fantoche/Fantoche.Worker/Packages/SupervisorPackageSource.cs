using System.Net;

namespace Fantoche.Worker.Packages;

/// <summary>
/// The packages the supervisor holds, downloaded from its API : what a worker, or an application
/// running a task itself, extracts into its <see cref="PackageCache"/>.
/// </summary>
public sealed class SupervisorPackageSource : IPackageSource
{
    private readonly HttpClient _client;

    /// <param name="client">Its base address being the supervisor.</param>
    public SupervisorPackageSource(HttpClient client)
    {
        _client = client;
    }

    public Task<Stream?> OpenPackageAsync(string id, Version version) => DownloadAsync(id, version, "file");

    public Task<Stream?> OpenSymbolsAsync(string id, Version version) => DownloadAsync(id, version, "symbols");

    /// <summary>
    /// Read whole before being handed over : a package is read back and forth while extracted,
    /// which a network stream can't do.
    /// </summary>
    private async Task<Stream?> DownloadAsync(string id, Version version, string file)
    {
        string url = $"packages/{Uri.EscapeDataString(id)}/{version}/{file}";
        using HttpResponseMessage response = await _client.GetAsync(url);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();

        var downloaded = new MemoryStream();
        await response.Content.CopyToAsync(downloaded);
        downloaded.Position = 0;
        return downloaded;
    }
}
