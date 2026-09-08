namespace Unclaimable;

public interface IChecker
{
    bool IsReserved(string? value);

    bool IsClaimable(string? value);

    Result Check(string? value);

    DetailedResult CheckDetailed(string? value, bool includeMessages = false);
}
