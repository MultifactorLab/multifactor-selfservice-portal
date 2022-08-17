using Microsoft.IdentityModel.Tokens;
using MultiFactor.SelfService.Linux.Portal.Extensions;

namespace MultiFactor.SelfService.Linux.Portal.Core.Authentication
{
    public static class TokenValidationParametersFactory
    {
        public static TokenValidationParameters GetParameters(IConfiguration config)
        {
            if (config is null) throw new ArgumentNullException(nameof(config));
            
            return new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = config.GetJsonWebKeySet().Keys,

                ValidateAudience = true,
                ValidAudience = config.GetPortalSettingsValue(x => x.MultiFactorApiSettings.ApiKey),

                ValidateIssuer = false,
                ValidateLifetime = true,
                ValidateTokenReplay = true,
            };
        }
    }
}
