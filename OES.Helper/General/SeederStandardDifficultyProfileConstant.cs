using OES.Helper.Enums;

namespace OES.Helper.General
{
    public static class SeederStandardDifficultyProfileConstant
    {
        public static readonly string Name = "ملف مستوى الصعوبة القياسي";

        public static readonly string Description = "وصف ملف مستوى الصعوبة القياسي";

        public static readonly List<(string Name, decimal FromDelta, decimal ToDelta, DeltaTypes DeltaType)> DifficultyLevels = [
            ("غير محدد الصعوبة", 0, 1.0m, DeltaTypes.Common),
            ("سهل", 0, 0.3m, DeltaTypes.Normal),
            ("متوسط", 0.31m, 0.6m, DeltaTypes.Normal),
            ("صعب", 0.61m, 1, DeltaTypes.Normal)
        ];
    }
}
