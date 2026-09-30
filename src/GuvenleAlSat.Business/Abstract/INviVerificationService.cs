using GuvenleAlSat.Core.Utilities.Results;

namespace GuvenleAlSat.Business.Abstract;

public interface INviVerificationService
{
    Task<IDataResult<bool>> VerifyAsync(string nationalId, string firstName, string lastName, int birthYear);
}