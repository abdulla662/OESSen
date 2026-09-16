
namespace OES.Helper.Dtos.ItemBank
{
    public class ItemBankStatisticsDto
    {
        public long QuestionsCount { get; set; }

        public List<ItemBankStatisticsQuestionTypeDto> ItemBankStatisticsType { get; set; } = [];

        public List<ItemBankStatisticsDifficultyLevelDto> ItemBankStatisticsDifficultyLevel { get; set; } = [];
    }
}
