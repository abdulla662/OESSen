namespace OES.Interface.Interfaces
{
    public interface IPromptTemplate<in TInput>
    {
        string BuildSystemPrompt(TInput input);

        string BuildUserPrompt(TInput input);
    }
}
