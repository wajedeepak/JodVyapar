namespace JodVyapar.Api.Common.Text;

/// <summary>Cleans text as it arrives on a request, so validation and storage see the same value.</summary>
public static class TextInput
{
    public static string Trim(string? value) => value?.Trim() ?? "";

    public static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
