using Microsoft.IdentityModel.Tokens;

namespace OpenAISelfhost.Service.Interface
{
    public interface IRsaKeyService
    {
        RsaSecurityKey GetCurrentSigningKey();
        IEnumerable<SecurityKey> GetValidationKeys();
    }
}
