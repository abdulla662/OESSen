using OES.Core.DatabaseObjects.CommonInterfaces;

namespace OES.Core.DatabaseObjects.Views.ItemBanks
{
    public class ItemBanksSummaryReportView : IDatabaseView
    {
        public string CreateOrReplaceCommand => @$"
            CREATE OR REPLACE VIEW `vw_ItemBanksSummaryReportView` AS
            SELECT
                ib.Id AS Id,
                ib.Name AS Name,
                ib.ItemBankSignature AS ItemBankSignature,
                (
                    SELECT COUNT(qm.Id)
                    FROM itembanks ib_sig
                    JOIN questionsmetadata qm
                        ON qm.ItemBankId = ib_sig.Id
                        AND qm.IsDeleted = 0
                        AND qm.IsActive = 1
                        AND qm.ParentId IS NULL
                    WHERE ib_sig.ItemBankSignature = ib.ItemBankSignature
                ) AS TotalQuestions
            FROM itembanks ib
            WHERE ib.ParentId IS NULL;
        ";
    }
}
