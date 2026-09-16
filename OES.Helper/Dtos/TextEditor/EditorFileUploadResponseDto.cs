namespace OES.Helper.Dtos.TextEditor
{
    public class EditorFileUploadResponseDto
    {
        public string ErrorMessage { get; set; } = string.Empty;

        public List<EditorFileUploadResultDto> Result { get; set; }
    }
}
