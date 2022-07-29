﻿using Microsoft.IdentityModel.Tokens;
using MultiFactor.SelfService.Linux.Portal.Core;

namespace MultiFactor.SelfService.Linux.Portal.Extensions
{
    public static class ConfigurationExtensions
    {
        public static JsonWebKeySet GetJsonWebKeySet(this IConfiguration config)
        {
            if (config is null) throw new ArgumentNullException(nameof(config));
            var value = config.GetValue<string>(Constants.TOKEN_VALIDATION);
            return new JsonWebKeySet(value);
        }
    }
}
