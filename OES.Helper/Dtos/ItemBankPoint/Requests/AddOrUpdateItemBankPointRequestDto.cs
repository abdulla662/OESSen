using OES.Helper.Enums;
using System.Text.Json.Serialization;

namespace OES.Helper.Dtos.ItemBankPoint.Requests
{
    public class AddOrUpdateItemBankPointRequestDto
    {
        public long PaperId { get; private set; }

        public QuestionSelectionType QuestionSelectionType { get; private set; }

        public List<long> ItemBankIds { get; private set; }

        public long PaperLanguageId { get; private set; }

        public long? PaperDifficultyProfileId { get; private set; }

        public int PaperQuestionsCount { get; private set; }

        public long OutputFormsCount { get; private set; }

        public bool AllowInstantResult { get; private set; }

        public QuestionDistributionTypeInForm QuestionDistributionTypeInForm { get; private set; }


        [JsonConstructor]
        public AddOrUpdateItemBankPointRequestDto(long paperId,
                                                  QuestionSelectionType questionSelectionType,
                                                  List<long> itemBankIds,
                                                  long paperLanguageId,
                                                  long? paperDifficultyProfileId,
                                                  int paperQuestionsCount,
                                                  long outputFormsCount,
                                                  bool allowInstantResult,
                                                  QuestionDistributionTypeInForm questionDistributionTypeInForm)
        {
            PaperId = paperId;
            QuestionSelectionType = questionSelectionType;
            ItemBankIds = itemBankIds;
            PaperLanguageId = paperLanguageId;
            PaperDifficultyProfileId = paperDifficultyProfileId;
            PaperQuestionsCount = paperQuestionsCount;
            OutputFormsCount = outputFormsCount;
            AllowInstantResult = allowInstantResult;
            QuestionDistributionTypeInForm = questionDistributionTypeInForm;
        }
    }
}
