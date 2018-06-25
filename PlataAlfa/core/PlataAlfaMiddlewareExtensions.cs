using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;

namespace PlataAlfa.core
{
    public static class PlataAlfaMiddlewareExtensions
    {
        public static IApplicationBuilder UsePlataAlfa(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<PlataAlfaMiddleware>();
        }
    }
}
