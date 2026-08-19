using AiUtility.AiBaseUtilityServices.Consts;
using AiUtility.GeminiUtilityServices.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace AiUtility.GeminiUtilityServices.Services
{
    public interface IGeminiApiClient
    {
        HttpClient HttpClient { get; init; }
        string ApiKey { get; init; }
        string BaseUrl { get; }
        string RequestUrl => $"{BaseUrl}?key={ApiKey}";

        Task<GeminiResponse> GenerateContentAsync(
            GeminiGenerateRequest request
        );
        Task<GeminiResponse> GenerateContentAsync(
            GeminiGenerateRequest request,
            CancellationToken ct = default
        );
    }
}
