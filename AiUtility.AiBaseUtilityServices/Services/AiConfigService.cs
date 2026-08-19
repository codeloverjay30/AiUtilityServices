using AiUtility.AiBaseUtilityServices.Services;
using FileStreamUtilityServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AiUtility.Configurations
{
    public class AiConfigService : IAiConfigService
    {
        private static readonly JsonSerializerOptions _options = AiUtility.Common.Options.JsonOptions.DefaultOptions;

        /// <summary>
        /// Path containing configurations used for AI model.
        /// </summary>
        public required string AiConfigPath { get; init; }

        public T ReadData<T>()
        {
            string json = FileUtility.ReadWithLock(AiConfigPath);
            var data = JsonSerializer.Deserialize<T>(json , _options);
            ArgumentNullException.ThrowIfNull(data);
            return data;
        }

        public ApiKeyConfig GetApiKeyConfig()
        {
            var data = ReadData<ApiKeyConfig>();
            ArgumentNullException.ThrowIfNull(data);
            return data;
        }

        public string GetApiKey()
        {
            var data = ReadData<ApiKeyConfig>()?.API_KEY;
            ArgumentNullException.ThrowIfNullOrWhiteSpace(data);
            return data;
        }
    }
}
