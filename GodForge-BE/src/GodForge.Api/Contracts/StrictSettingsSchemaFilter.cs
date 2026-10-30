using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace GodForge.Api.Contracts;

public sealed class StrictSettingsSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type.GetCustomAttribute<JsonUnmappedMemberHandlingAttribute>()?.UnmappedMemberHandling != JsonUnmappedMemberHandling.Disallow)
            return;
        schema.AdditionalPropertiesAllowed = false;
        foreach (var property in context.Type.GetProperties().Where(p => p.IsDefined(typeof(RequiredMemberAttribute))))
        {
            var name = System.Text.Json.JsonNamingPolicy.CamelCase.ConvertName(property.Name);
            if (schema.Properties.ContainsKey(name)) schema.Required.Add(name);
        }
        if (context.Type == typeof(UserPreferencesRequest))
        {
            schema.Properties["theme"].Enum = [new Microsoft.OpenApi.Any.OpenApiString("light"), new Microsoft.OpenApi.Any.OpenApiString("dark")];
            schema.Properties["notificationDigest"].Enum = [new Microsoft.OpenApi.Any.OpenApiString("off"), new Microsoft.OpenApi.Any.OpenApiString("daily"), new Microsoft.OpenApi.Any.OpenApiString("weekly")];
        }
    }
}
