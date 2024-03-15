﻿using Brand.Template.Api;
using Brand.Template.Api.Filter;

using Microsoft.Extensions.Options;

namespace Presentation.Middleware;

internal sealed class AuthMiddleware(
    IOptionsSnapshot<Settings.Auth> securitySettings
) : IMiddleware
{
    private Settings.Auth _auth => securitySettings.Value;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        bool requisicaoAutorizada = true;

        bool apiPath = context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase);

        if (_auth.Enabled && apiPath)
        {
            string? apiKey = ExtrairApiKey(context.Request.Headers);

            if (string.IsNullOrEmpty(apiKey) || !_auth.ApiKeys.ContainsValue(apiKey))
                requisicaoAutorizada = false;
        }

        if (!requisicaoAutorizada)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;

            await context.Response.WriteAsJsonAsync(
                new Response<object?>(null, ["Preencha o api-key header"])
            );

            return;
        }

        await next(context);
    }

    private string? ExtrairApiKey(IHeaderDictionary headers) =>
        headers
            .FirstOrDefault(h => _auth.Headers.Contains(h.Key.ToLowerInvariant()))
            .Value;
}