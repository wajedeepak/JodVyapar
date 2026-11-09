using System.Security.Cryptography;
using System.Text;

namespace JodVyapar.Api.Tests;

/// <summary>
/// The fixed Ids used by Database/02_SeedData.sql: MD5 of "jodvyapar:&lt;key&gt;", e.g. "state:MH"
/// or "city:MH:Mumbai". Lets tests refer to seeded rows without hard-coding GUIDs.
/// </summary>
public static class SeedIds
{
    public static Guid For(string key) => new(MD5.HashData(Encoding.UTF8.GetBytes($"jodvyapar:{key}")));
}
