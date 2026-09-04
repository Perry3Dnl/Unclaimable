namespace Unclaimable;

public interface IUnclaimableChecker
{
    bool IsReserved(string? value);

    bool IsClaimable(string? value);

    UnclaimableResult Check(string? value);
}
