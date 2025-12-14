using System.Security.Cryptography;
using System.Text;

namespace Dnd35.SpellCards.Infrastructure;

public sealed class HttpCache
{
    private readonly HttpClient _http;
    private readonly string _cacheDir;

    public HttpCache(HttpClient http, string cacheDir)
    {
        _http = http;
        _cacheDir = cacheDir;
        Directory.CreateDirectory(_cacheDir);
    }

    public async Task<string> GetStringCachedAsync(string url, CancellationToken ct)
    {
        var file = Path.Combine(_cacheDir, Sha1(url) + ".html");
        if (File.Exists(file))
            return await File.ReadAllTextAsync(file, ct);

        var html = await _http.GetStringAsync(url, ct);
        await File.WriteAllTextAsync(file, html, ct);
        return html;
    }

    private static string Sha1(string s)
    {
        using var sha1 = SHA1.Create();
        var bytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(s));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
