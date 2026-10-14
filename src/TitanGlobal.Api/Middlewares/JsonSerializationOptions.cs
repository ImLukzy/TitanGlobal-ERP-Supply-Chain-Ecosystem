using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace TitanGlobal.Api.Middlewares;

public static class JsonSerializationOptions
{
    public static JsonSerializerOptions ErrorResponses { get; } = new JsonSerializerOptions
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };
}
