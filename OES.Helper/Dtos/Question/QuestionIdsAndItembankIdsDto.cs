using OES.Helper.Dtos.Paper.ImportQuestionsRequestDto;

namespace OES.Helper.Dtos.Question
{
    public class QuestionIdsAndItemBankIdsDto
    {
        public List<long> QuestionIds { get; set; } = [];

        public List<long> ItemBankIds { get; set; } = [];

        public ValidateExcelSheetQuestionsDto ValidateExcelSheetQuestionsDto { get; set; }
    }
}
