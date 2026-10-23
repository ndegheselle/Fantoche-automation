using System.Net;
using Fantoche.Worker.Packages;

namespace Fantoche.Services.Remote;

/// <summary>
/// The packages the supervisor holds, downloaded from its API : what a worker extracts into its
/// <see cref="PackageCache"/>.
/// </summary>
public sealed class RemotePackageSource : IPackageSource
{
    private readonly HttpClient _client;

    /// <param name="client">Its base address being the supervisor.</param>
    public RemotePackageSource(HttpClient client)
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
