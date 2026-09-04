namespace Unclaimable;

public interface IUnclaimableChecker
{
    bool IsReserved(string? value);

    bool IsClaimable(string? value);

    UnclaimableResult Check(string? value);

    UnclaimableDetailedResult CheckDetailed(string? value, bool includeMessages = false);
}
