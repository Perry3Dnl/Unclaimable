namespace Unclaimable;

/// <summary>
/// Runtime-adjustable character policy used by Unclaimable.
/// Changes are process-local and thread-safe.
/// </summary>
public interface IUnclaimablePolicy
{
    IReadOnlyCollection<string> BlockedCharacters { get; }

    void BlockCharacter(string value);

    void BlockCharacters(params string[] values);

    bool AllowCharacter(string value);

    void AllowCharacters(params string[] values);

    bool IsCharacterBlocked(string value);

    bool IsCharacterExplicitlyAllowed(string value);
}
