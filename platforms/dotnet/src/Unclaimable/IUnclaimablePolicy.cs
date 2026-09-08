namespace Unclaimable;

/// <summary>
/// Defines the runtime-adjustable character policy used by Unclaimable.
/// Implementations may change policy state while a checker remains active.
/// </summary>
public interface IUnclaimablePolicy
{
    /// <summary>
    /// Gets the currently blocked Unicode characters after runtime allowances are applied.
    /// </summary>
    IReadOnlyCollection<string> BlockedCharacters { get; }

    /// <summary>
    /// Blocks a Unicode character and removes any explicit runtime allowance for it.
    /// </summary>
    /// <param name="value">Exactly one Unicode character to block.</param>
    void BlockCharacter(string value);

    /// <summary>
    /// Blocks one or more Unicode characters.
    /// </summary>
    /// <param name="values">The characters to block. Each value must contain exactly one Unicode character.</param>
    void BlockCharacters(params string[] values);

    /// <summary>
    /// Explicitly allows a Unicode character, including a character that is blocked by the built-in policy.
    /// </summary>
    /// <param name="value">Exactly one Unicode character to allow.</param>
    /// <returns><see langword="true"/> when a new allowance was added; otherwise, <see langword="false"/>.</returns>
    bool AllowCharacter(string value);

    /// <summary>
    /// Explicitly allows one or more Unicode characters.
    /// </summary>
    /// <param name="values">The characters to allow. Each value must contain exactly one Unicode character.</param>
    void AllowCharacters(params string[] values);

    /// <summary>
    /// Determines whether a Unicode character is currently blocked.
    /// </summary>
    /// <param name="value">Exactly one Unicode character to inspect.</param>
    /// <returns><see langword="true"/> when the character is blocked; otherwise, <see langword="false"/>.</returns>
    bool IsCharacterBlocked(string value);

    /// <summary>
    /// Determines whether a Unicode character has been explicitly allowed at runtime.
    /// </summary>
    /// <param name="value">Exactly one Unicode character to inspect.</param>
    /// <returns><see langword="true"/> when the character is explicitly allowed; otherwise, <see langword="false"/>.</returns>
    bool IsCharacterExplicitlyAllowed(string value);
}
