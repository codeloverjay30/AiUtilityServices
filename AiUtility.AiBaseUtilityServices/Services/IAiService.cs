using AiUtility.Configurations;
using System;
using System.Collections.Generic;
using System.Text;

namespace AiUtility.AiBaseUtilityServices.Services
{
    public interface IAiService
    {
        string ConfigPath { get; init; }
        ApiKeyConfig GetApiKeyConfig();
    }
}
