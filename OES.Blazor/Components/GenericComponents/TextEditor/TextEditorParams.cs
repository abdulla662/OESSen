namespace OES.Blazor.Components.GenericComponents.TextEditor
{
    public class TextEditorParams
    {
        public Guid ComponentGuid { get; private init; } = Guid.NewGuid();

        // This ID acts as a general identifier for any concrete data represented by this object.
        public long CustomId { get; set; }

        public TextEditor TextEditor { get; set; }

        public string InitialContent { get; set; } = string.Empty;

        public bool WithMathChemPanel { get; init; }

        public bool WithFileManagerPanel { get; init; }

        public string Label { get; set; } = string.Empty;

        public bool ErrorShown { get; set; }

        public string ErrorText { get; set; } = string.Empty;

        public bool RequiredAsteriskShown { get; init; }

        public bool EmbedImagesAsBase64 { get; init; } = false;

        public bool IsReadOnly { get; set; } = false;


        public async Task<string> GetTextEditorContentAsync() => await TextEditor.ObtainTextEditorContentAsync();

        public async Task SetTextEditorContentAsync(string content) => await TextEditor.SetTextEditorContentAsync(content);

        public async ValueTask DisposeJsEditorAsync() => await TextEditor.DisposeJsEditorAsync();
    }
}
