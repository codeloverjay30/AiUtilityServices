using System;
using System.Collections.Generic;
using System.Text;
using AiUtility.Configurations;

namespace AiUtility.AiBaseUtilityServices.Services
{
    public interface IAiConfigService
    {
        string AiConfigPath { get; init; }

        T ReadData<T>();

        ApiKeyConfig GetApiKeyConfig();

        string GetApiKey();
    }
}
