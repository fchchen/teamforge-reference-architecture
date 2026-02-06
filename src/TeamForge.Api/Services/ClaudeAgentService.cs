using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TeamForge.Api.Models;

namespace TeamForge.Api.Services;

public class ClaudeAgentService : IAiAgentService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly ILogger<ClaudeAgentService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public ClaudeAgentService(HttpClient httpClient, IConfiguration config, ILogger<ClaudeAgentService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _apiKey = config["Claude:ApiKey"] ?? throw new InvalidOperationException("Claude:ApiKey not configured");
        _model = config["Claude:Model"] ?? "claude-sonnet-4-5-20250929";
    }

    public async Task<OnboardingPreview> GenerateOnboardingConfigAsync(
        string companyName,
        string companyDescription,
        CancellationToken cancellationToken = default)
    {
        var toolDefinition = new
        {
            name = "generate_onboarding_config",
            description = "Generate a complete onboarding configuration for a new tenant based on company description",
            input_schema = new
            {
                type = "object",
                properties = new Dictionary<string, object>
                {
                    ["suggestedBranding"] = new
                    {
                        type = "object",
                        properties = new Dictionary<string, object>
                        {
                            ["primaryColor"] = new { type = "string", description = "Hex color for primary brand color" },
                            ["secondaryColor"] = new { type = "string", description = "Hex color for secondary brand color" },
                            ["accentColor"] = new { type = "string", description = "Hex color for accent elements" },
                            ["backgroundColor"] = new { type = "string", description = "Hex color for page background" },
                            ["textColor"] = new { type = "string", description = "Hex color for text" },
                            ["fontFamily"] = new { type = "string", description = "Font family name" },
                            ["tagLine"] = new { type = "string", description = "Company tagline" }
                        },
                        required = new[] { "primaryColor", "secondaryColor", "accentColor", "backgroundColor", "textColor", "fontFamily", "tagLine" }
                    },
                    ["roles"] = new
                    {
                        type = "array",
                        items = new { type = "string" },
                        description = "Role names for the organization (always include Admin)"
                    },
                    ["teams"] = new
                    {
                        type = "array",
                        items = new
                        {
                            type = "object",
                            properties = new Dictionary<string, object>
                            {
                                ["name"] = new { type = "string" },
                                ["description"] = new { type = "string" }
                            },
                            required = new[] { "name", "description" }
                        },
                        description = "Suggested team structure"
                    },
                    ["projectCategories"] = new
                    {
                        type = "array",
                        items = new { type = "string" },
                        description = "Categories for organizing projects"
                    },
                    ["welcomeAnnouncement"] = new
                    {
                        type = "string",
                        description = "Welcome message for the company portal"
                    }
                },
                required = new[] { "suggestedBranding", "roles", "teams", "projectCategories", "welcomeAnnouncement" }
            }
        };

        var requestBody = new
        {
            model = _model,
            max_tokens = 1024,
            tools = new[] { toolDefinition },
            tool_choice = new { type = "tool", name = "generate_onboarding_config" },
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = $"Generate an onboarding configuration for a company called \"{companyName}\". " +
                              $"Here is their description: \"{companyDescription}\". " +
                              "Choose branding colors that match the company's industry and personality. " +
                              "Create relevant teams and project categories based on their description. " +
                              "Write a warm, professional welcome announcement."
                }
            }
        };

        var json = JsonSerializer.Serialize(requestBody, JsonOptions);
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        request.Headers.Add("x-api-key", _apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");

        _logger.LogInformation("Calling Claude API for onboarding config generation for {CompanyName}", companyName);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Claude API error: {StatusCode} - {Body}", response.StatusCode, responseBody);
            throw new InvalidOperationException($"Claude API returned {response.StatusCode}");
        }

        return ParseToolUseResponse(responseBody);
    }

    private OnboardingPreview ParseToolUseResponse(string responseBody)
    {
        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        var content = root.GetProperty("content");
        foreach (var block in content.EnumerateArray())
        {
            if (block.GetProperty("type").GetString() == "tool_use")
            {
                var input = block.GetProperty("input");
                return MapToPreview(input);
            }
        }

        throw new InvalidOperationException("No tool_use block found in Claude response");
    }

    private static OnboardingPreview MapToPreview(JsonElement input)
    {
        var branding = input.GetProperty("suggestedBranding");

        return new OnboardingPreview
        {
            Branding = new SuggestedBranding
            {
                PrimaryColor = branding.GetProperty("primaryColor").GetString() ?? "#1976d2",
                SecondaryColor = branding.GetProperty("secondaryColor").GetString() ?? "#ff9800",
                AccentColor = branding.GetProperty("accentColor").GetString() ?? "#4caf50",
                BackgroundColor = branding.GetProperty("backgroundColor").GetString() ?? "#fafafa",
                TextColor = branding.GetProperty("textColor").GetString() ?? "#212121",
                FontFamily = branding.GetProperty("fontFamily").GetString() ?? "Roboto",
                TagLine = branding.GetProperty("tagLine").GetString() ?? string.Empty
            },
            Roles = input.GetProperty("roles").EnumerateArray()
                .Select(r => r.GetString() ?? "Member").ToList(),
            Teams = input.GetProperty("teams").EnumerateArray()
                .Select(t => new SuggestedTeam
                {
                    Name = t.GetProperty("name").GetString() ?? string.Empty,
                    Description = t.GetProperty("description").GetString() ?? string.Empty
                }).ToList(),
            ProjectCategories = input.GetProperty("projectCategories").EnumerateArray()
                .Select(c => c.GetString() ?? string.Empty).ToList(),
            WelcomeAnnouncement = input.GetProperty("welcomeAnnouncement").GetString() ?? string.Empty
        };
    }
}
