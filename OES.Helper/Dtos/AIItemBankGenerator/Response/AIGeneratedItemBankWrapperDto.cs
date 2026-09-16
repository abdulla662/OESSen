namespace OES.Helper.Dtos.AIItemBankGenerator.Response
{
    public sealed record AIGeneratedItemBankWrapperDto
    {
        public AIGeneratedItemBankNodeDto Root { get; init; }
    }

    public sealed record AIGeneratedItemBankNodeDto
    {
        public string Name { get; init; }

        public string Description { get; init; } = "";

        public float Hours { get; init; } = 0;

        public long? LevelId { get; init; }

        public string LevelName { get; init; }

        public List<AIGeneratedItemBankNodeDto> Children { get; init; } = [];
    }
}
