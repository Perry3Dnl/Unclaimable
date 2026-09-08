namespace Unclaimable;

/// <summary>
/// Provides the default process-local, thread-safe runtime character policy used by Unclaimable.
/// Built-in blocked characters can be relaxed or extended without rebuilding an active checker.
/// </summary>
public sealed class UnclaimablePolicy : IUnclaimablePolicy
{
    private static readonly string[] BuiltInBlockedCharacters = { "-", "_" };

    private readonly object _sync = new object();
    private readonly HashSet<string> _blocked = new HashSet<string>(StringComparer.Ordinal);
    private readonly HashSet<string> _allowed = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a policy with only the built-in blocked characters.
    /// </summary>
    public UnclaimablePolicy()
    {
    }

    /// <summary>
    /// Initializes a policy with additional blocked Unicode characters.
    /// </summary>
    /// <param name="additionalBlockedCharacters">Characters to block in addition to the built-in policy.</param>
    public UnclaimablePolicy(IEnumerable<string> additionalBlockedCharacters)
    {
        if (additionalBlockedCharacters is null)
        {
            throw new ArgumentNullException(nameof(additionalBlockedCharacters));
        }

        foreach (var value in additionalBlockedCharacters)
        {
            ValidateCharacter(value, nameof(additionalBlockedCharacters));
            _blocked.Add(value);
        }
    }

    /// <summary>
    /// Gets a snapshot of the currently blocked Unicode characters after runtime allowances are applied.
    /// </summary>
    public IReadOnlyCollection<string> BlockedCharacters
    {
        get
        {
            lock (_sync)
            {
                return BuiltInBlockedCharacters
                    .Concat(_blocked)
                    .Where(value => !_allowed.Contains(value))
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(value => value, StringComparer.Ordinal)
                    .ToArray();
            }
        }
    }

    /// <summary>
    /// Blocks a Unicode character and removes any explicit runtime allowance for it.
    /// </summary>
    /// <param name="value">Exactly one Unicode character to block.</param>
    public void BlockCharacter(string value)
    {
        ValidateCharacter(value, nameof(value));

        lock (_sync)
        {
            _allowed.Remove(value);
            _blocked.Add(value);
        }
    }

    /// <summary>
    /// Blocks one or more Unicode characters as a single validated update.
    /// </summary>
    /// <param name="values">The characters to block. Each value must contain exactly one Unicode character.</param>
    public void BlockCharacters(params string[] values)
    {
        if (values is null)
        {
            throw new ArgumentNullException(nameof(values));
        }

        foreach (var value in values)
        {
            ValidateCharacter(value, nameof(values));
        }

        lock (_sync)
        {
            foreach (var value in values)
            {
                _allowed.Remove(value);
                _blocked.Add(value);
            }
        }
    }

    /// <summary>
    /// Explicitly allows a Unicode character, including a character blocked by the built-in policy.
    /// </summary>
    /// <param name="value">Exactly one Unicode character to allow.</param>
    /// <returns><see langword="true"/> when a new allowance was added; otherwise, <see langword="false"/>.</returns>
    public bool AllowCharacter(string value)
    {
        ValidateCharacter(value, nameof(value));

        lock (_sync)
        {
            _blocked.Remove(value);
            return _allowed.Add(value);
        }
    }

    /// <summary>
    /// Explicitly allows one or more Unicode characters as a single validated update.
    /// </summary>
    /// <param name="values">The characters to allow. Each value must contain exactly one Unicode character.</param>
    public void AllowCharacters(params string[] values)
    {
        if (values is null)
        {
            throw new ArgumentNullException(nameof(values));
        }

        foreach (var value in values)
        {
            ValidateCharacter(value, nameof(values));
        }

        lock (_sync)
        {
            foreach (var value in values)
            {
                _blocked.Remove(value);
                _allowed.Add(value);
            }
        }
    }

    /// <summary>
    /// Determines whether a Unicode character is currently blocked.
    /// </summary>
    /// <param name="value">Exactly one Unicode character to inspect.</param>
    /// <returns><see langword="true"/> when the character is blocked; otherwise, <see langword="false"/>.</returns>
    public bool IsCharacterBlocked(string value)
    {
        ValidateCharacter(value, nameof(value));

        lock (_sync)
        {
            if (_allowed.Contains(value))
            {
                return false;
            }

            return _blocked.Contains(value)
                   || Array.IndexOf(BuiltInBlockedCharacters, value) >= 0;
        }
    }

    /// <summary>
    /// Determines whether a Unicode character has been explicitly allowed at runtime.
    /// </summary>
    /// <param name="value">Exactly one Unicode character to inspect.</param>
    /// <returns><see langword="true"/> when the character is explicitly allowed; otherwise, <see langword="false"/>.</returns>
    public bool IsCharacterExplicitlyAllowed(string value)
    {
        ValidateCharacter(value, nameof(value));

        lock (_sync)
        {
            return _allowed.Contains(value);
        }
    }

    private static void ValidateCharacter(string value, string parameterName)
    {
        if (string.IsNullOrEmpty(value))
        {
            throw new ArgumentException("A blocked character cannot be null or empty.", parameterName);
        }

        if (value.Length == 1)
        {
            if (char.IsSurrogate(value[0]))
            {
                throw new ArgumentException("A surrogate must be supplied as a complete Unicode scalar value.", parameterName);
            }

            return;
        }

        if (value.Length == 2 && char.IsSurrogatePair(value[0], value[1]))
        {
            return;
        }

        throw new ArgumentException("Values must contain exactly one Unicode character.", parameterName);
    }
}
