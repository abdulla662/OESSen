using OES.Helper.General;
namespace OES.Helper.Dtos.ItemBank
{
    public sealed record ItemBankPaginationSearchRequest(PaginationSearchModel PaginationSearch, bool IsFromItemBankAI);
}
