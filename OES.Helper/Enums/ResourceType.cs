using OES.Helper.General;

namespace OES.Helper.Enums
{
    [Flags]
    public enum ResourceType
    {
        [RolePrefix("All")]
        All = 0,
        [RolePrefix("Question")]
        Questions = 1,
        [RolePrefix("Paper")]
        Papers = 2,
        [RolePrefix("Schedule")]
        Schedule = 3,
        [RolePrefix("ItemBank")]
        ItemBank = 4,
        [RolePrefix("Ilo")]
        Ilo = 5,
        [RolePrefix("Result")]
        Result = 6,
        [RolePrefix("Block")]
        Block = 7,
        [RolePrefix("Equation")]
        Equation = 8,
        [RolePrefix("Candidate")]
        Candidate = 9,
        [RolePrefix("Transition")]
        Transition = 10,
        [RolePrefix("Venue")]
        Venue = 11,
        [RolePrefix("FileManger")]
        FileManger = 12,
        [RolePrefix("QuestionCategory")]
        QuestionCategory = 13,
        [RolePrefix("DeltaType")]
        DeltaType = 14,
        [RolePrefix("DifficultyProfile")]
        DifficultyProfile = 15,
        [RolePrefix("DifficultyLevel")]
        DifficultyLevel = 16,
        [RolePrefix("Language")]
        Language = 17,
        [RolePrefix("Material")]
        Material = 18,
        [RolePrefix("MediaConfiguration")]
        MediaConfiguration = 19 ,
        [RolePrefix("QualityCheckCommittee")]
        QualityCheckCommittee = 20,
        [RolePrefix("Configuration")]
        Configurations = 21,
        [RolePrefix("Report")]
        Report = 22,
        [RolePrefix("CbtAutoSettings")]
        CbtAutoSettings = 23,
        [RolePrefix("SpecialCases")]
        SpecialCases = 24,
    }
}
