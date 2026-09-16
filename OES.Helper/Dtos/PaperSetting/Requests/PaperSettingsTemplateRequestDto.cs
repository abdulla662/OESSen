using OES.Helper.Dtos.PaperSetting.Responses;

namespace OES.Helper.Dtos.PaperSetting.Requests
{
    public class PaperSettingsTemplateRequestDto
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public GetPaperSettingsResponseDto PaperSettingsResponseDto { get; set; } = new();
    }
}
