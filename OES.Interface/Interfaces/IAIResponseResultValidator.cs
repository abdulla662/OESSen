
using OES.Helper.Dtos.Validation;

namespace OES.Interface.Interfaces
{
    public interface IAIResponseResultValidator<in TResult, in TContext>
    {
        AIResponseValidationResult Validate(TResult result, TContext context);
    }
}
