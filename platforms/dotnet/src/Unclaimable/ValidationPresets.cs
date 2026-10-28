namespace Unclaimable;

/// <summary>
/// Explicitly selected configurations for common identifier use cases.
/// These factories never modify <see cref="Options"/> defaults or shared state.
/// </summary>
public static class ValidationPresets
{
    /// <summary>Returns a fresh instance of the unchanged default username policy.</summary>
    public static Options Username() => new Options();

    /// <summary>
    /// Returns a display-name policy with a wider length range and a relaxed
    /// whitespace rule. All other default protections remain enabled.
    /// </summary>
    public static Options DisplayName() => new Options
    {
        MinimumLength = 2,
        MaximumLength = 64
    }.DisableRule(Rule.Whitespace).AllowCharacters(" ");

    /// <summary>
    /// Returns an organization-name policy with a longer length range and
    /// the standard reserved-name and abuse protections.
    /// </summary>
    public static Options OrganizationName() => new Options
    {
        MinimumLength = 2,
        MaximumLength = 80
    }.DisableRule(Rule.Whitespace);

    /// <summary>
    /// Returns a short ASCII game-tag policy. Internationalized tags can use
    /// <see cref="Username"/> or configure <see cref="Options.AsciiOnly"/> explicitly.
    /// </summary>
    public static Options GameTag() => new Options
    {
        MinimumLength = 3,
        MaximumLength = 24,
        AsciiOnly = true
    };
}
