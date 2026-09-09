namespace Unclaimable;

/// <summary>
/// Default in-memory runtime policy. Built-in blocked characters can be relaxed or extended at runtime.
/// Runtime updates remain visible to every checker using the same policy instance.
/// </summary>
public sealed class Policy : IPolicy
{
    private static readonly string[] BuiltInBlockedCharacters = { "-", "_" };

    private readonly object _sync = new object();
    private readonly HashSet<string> _blocked = new HashSet<string>(StringComparer.Ordinal);
    private readonly HashSet<string> _allowed = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>Creates a policy with only the built-in blocked characters.</summary>
    public Policy()
    {
    }

    /// <summary>Creates a policy and adds application-specific blocked Unicode scalar values.</summary>
    public Policy(IEnumerable<string> additionalBlockedCharacters)
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

    /// <summary>Gets currently blocked characters after explicit allowances are applied.</summary>
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

    /// <summary>Blocks one Unicode scalar value.</summary>
    public void BlockCharacter(string value)
    {
        ValidateCharacter(value, nameof(value));

        lock (_sync)
        {
            _allowed.Remove(value);
            _blocked.Add(value);
        }
    }

    /// <summary>Blocks one or more Unicode scalar values.</summary>
    public void BlockCharacters(params string[] values)
    {
        if (values is null)
        {
            throw new ArgumentNullException(nameof(values));
        }

        foreach (var value in values)
        {
            BlockCharacter(value);
        }
    }

    /// <summary>Explicitly allows one Unicode scalar value and returns whether the allowed set changed.</summary>
    public bool AllowCharacter(string value)
    {
        ValidateCharacter(value, nameof(value));

        lock (_sync)
        {
            _blocked.Remove(value);
            return _allowed.Add(value);
        }
    }

    /// <summary>Explicitly allows one or more Unicode scalar values.</summary>
    public void AllowCharacters(params string[] values)
    {
        if (values is null)
        {
            throw new ArgumentNullException(nameof(values));
        }

        foreach (var value in values)
        {
            AllowCharacter(value);
        }
    }

    /// <summary>Returns whether a Unicode scalar value is currently blocked.</summary>
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

    /// <summary>Returns whether a Unicode scalar value is explicitly allowed.</summary>
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
