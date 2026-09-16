using OES.Helper.Dtos.UploadFiles;

namespace OES.Helper.General
{
    public class MudTableElementComparer : IEqualityComparer<UploadQuestionDetailsDto>
    {
        public bool Equals(UploadQuestionDetailsDto a, UploadQuestionDetailsDto b) => a?.Id == b?.Id;

        public int GetHashCode(UploadQuestionDetailsDto x) => HashCode.Combine(x?.Id);
    }
}
