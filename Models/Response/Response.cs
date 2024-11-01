using Newtonsoft.Json;
using static UserAuthentication_ASPNET.Models.Utils.Error;

namespace UserAuthentication_ASPNET.Models.Response
{
    public class ApiResponse<T>
    {
        public string Status { get; init; } = null!;
        public string Message { get; init; } = null!;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public T? Data { get; init; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public ErrorType? ErrorType { get; init; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, string>? ValidationErrors { get; init; }
    }
}