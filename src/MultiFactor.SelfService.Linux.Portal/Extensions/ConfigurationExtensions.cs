using Microsoft.IdentityModel.Tokens;
using MultiFactor.SelfService.Linux.Portal.Core;

namespace MultiFactor.SelfService.Linux.Portal.Extensions
{
    public static class ConfigurationExtensions
    {
        public static JsonWebKeySet GetJsonWebKeySet(this IConfiguration config)
        {
            ArgumentNullException.ThrowIfNull(config);
            var value = config.GetValue<string>(Constants.TOKEN_VALIDATION);
            return new JsonWebKeySet(value);
        }

        public static string GetEnvironment(this IConfiguration config)
        {
            ArgumentNullException.ThrowIfNull(config);
            return config.GetValue<string>(Constants.ENVIRONMENT_KEY);
        }
    }
}
