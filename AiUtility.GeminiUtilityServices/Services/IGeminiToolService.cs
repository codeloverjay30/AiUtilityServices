using AiUtility.GeminiUtilityServices.Models;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace AiUtility.GeminiUtilityServices.Services
{
    public interface IGeminiToolService
    {
        void SyncToolsToRequest(GeminiGenerateRequest request);
    }
}
