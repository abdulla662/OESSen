using SharedHelper.Enums;

namespace OES.Helper.Dtos.UserPapersDto
{
    public class GetUserPapersDto
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public PaperType? Type { get; set; }
    }
}
