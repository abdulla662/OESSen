using OES.Core.DatabaseObjects.CommonInterfaces;

namespace OES.Core.DatabaseObjects.Views.Questions
{
    public class CreatingQuestionsAutoSelectionSummaryView : IDatabaseView
    {
        public string CreateOrReplaceCommand => @"
            CREATE OR REPLACE VIEW vw_autoquestionsummary AS
            SELECT 
                pm.Id AS PaperId,
                pm.Name AS PaperName,
                sec.Id AS SectionId,
                sec.Name AS SectionName,
                sec.OrderId AS SectionOrderId,
                ib.Id AS ItemBankId,
                ib.Name AS ItemBankName,
                qt.Id AS QuestionTypeId,
                qt.Name AS QuestionTypeName,
                dl.Id AS DifficultyLevelId,
                dl.Name AS DifficultyLevelName,
                apiqs.SelectedCount AS QuestionsCount
            FROM
                papermetadata pm
                JOIN sections sec ON (sec.PaperId = pm.Id)
                JOIN AutoPaperItemBankQuestionSections apiqs ON (apiqs.SectionId = sec.Id)
                JOIN itembankpoints ibp ON ((ibp.Id = apiqs.ItemBankPointId) AND (ibp.PaperId = pm.Id))
                JOIN itembanks ib ON (ib.Id = ibp.ItemBankId)
                LEFT JOIN questiontypes qt ON (qt.Id = apiqs.QuestionTypeID)
                LEFT JOIN difficultylevels dl ON (dl.Id = apiqs.DifficultyLevelID);
        ";
    }
}
