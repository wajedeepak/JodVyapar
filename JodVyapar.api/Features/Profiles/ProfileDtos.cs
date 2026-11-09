namespace JodVyapar.Api.Features.Profiles;

/// <summary>
/// Body of PUT /profile/me. Email and roles are read-only here. Text is trimmed on the way in, so
/// validation and storage both see the trimmed value; a blank GSTIN or About becomes null.
/// </summary>
public sealed class UpdateProfileRequest
{
    private readonly string _fullName = "";
    private readonly string _companyName = "";
    private readonly string _mobile = "";
    private readonly string? _gstin;
    private readonly string? _about;

    public string FullName { get => _fullName; init => _fullName = value?.Trim() ?? ""; }
    public string CompanyName { get => _companyName; init => _companyName = value?.Trim() ?? ""; }
    public string Mobile { get => _mobile; init => _mobile = value?.Trim() ?? ""; }
    public Guid? StateId { get; init; }
    public Guid? CityId { get; init; }

    /// <summary>Optional. Stored upper-case.</summary>
    public string? Gstin { get => _gstin; init => _gstin = NullIfBlank(value)?.ToUpperInvariant(); }

    /// <summary>Optional, max 500 characters.</summary>
    public string? About { get => _about; init => _about = NullIfBlank(value); }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

/// <summary>Body of POST /profile/me/roles.</summary>
public sealed class AddRoleRequest
{
    /// <summary>"Buyer" or "Seller".</summary>
    public string Role { get; init; } = "";
}

/// <summary>The signed-in user's own profile, including private contact details.</summary>
public sealed record MyProfileDto(
    Guid Id,
    string FullName,
    string Email,
    IReadOnlyList<string> Roles,
    bool IsEmailVerified,
    DateTime MemberSince,
    MyCompanyDto? Company);

/// <summary>Null on MyProfileDto for Admin-only users who haven't filled in company details yet.</summary>
public sealed record MyCompanyDto(
    string CompanyName,
    Guid StateId,
    string StateName,
    Guid CityId,
    string CityName,
    string Mobile,
    string? Gstin,
    string? About,
    bool IsMobileVerified,
    bool IsGstinVerified);

/// <summary>
/// What other signed-in users see. Deliberately has no email, mobile or GSTIN number.
/// </summary>
public sealed record PublicProfileDto(
    Guid Id,
    string FullName,
    string CompanyName,
    string CityName,
    string StateName,
    IReadOnlyList<string> Roles,
    string? About,
    bool IsEmailVerified,
    bool IsGstinVerified,
    DateTime MemberSince);

/// <summary>Machine-readable "code" values added to ProblemDetails responses by the profile endpoints.</summary>
public static class ProfileErrorCodes
{
    public const string CompanyProfileRequired = "COMPANY_PROFILE_REQUIRED";
}
