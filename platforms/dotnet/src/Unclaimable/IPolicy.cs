namespace Unclaimable;

/// <summary>
/// Runtime-adjustable character policy used by Unclaimable.
/// Changes are process-local, thread-safe, and remain live for checkers that reference this policy.
/// </summary>
public interface IPolicy
{
    /// <summary>Gets currently blocked Unicode scalar values after explicit allowances are applied.</summary>
    IReadOnlyCollection<string> BlockedCharacters { get; }

    /// <summary>Blocks one Unicode scalar value.</summary>
    void BlockCharacter(string value);

    /// <summary>Blocks one or more Unicode scalar values.</summary>
    void BlockCharacters(params string[] values);

    /// <summary>Explicitly allows one Unicode scalar value and returns whether the allowed set changed.</summary>
    bool AllowCharacter(string value);

    /// <summary>Explicitly allows one or more Unicode scalar values.</summary>
    void AllowCharacters(params string[] values);

    /// <summary>Returns whether a Unicode scalar value is currently blocked.</summary>
    bool IsCharacterBlocked(string value);

    /// <summary>Returns whether a Unicode scalar value is explicitly allowed.</summary>
    bool IsCharacterExplicitlyAllowed(string value);
}
