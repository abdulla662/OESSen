namespace OES.Helper.General
{
    public static class ListHelpers
    {
        public static bool HaveCommonValues(List<string> list1, List<string> list2)
        {
            var intersectionResult = list1.Intersect(list2);

            return intersectionResult.Any();
        }

        public static string SanitizeCode(string value)
        {
            return value?.Replace(" ", "-");
        }
    }
}
