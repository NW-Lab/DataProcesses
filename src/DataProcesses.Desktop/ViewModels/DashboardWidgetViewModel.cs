using System.Text.Json;
using System.Text.Json.Nodes;

namespace DataProcesses.Desktop.ViewModels;

public sealed class DashboardWidgetViewModel : ViewModelBase
{
    private const int DefaultGridWidth = 2;
    private const int DefaultGridHeight = 2;
    private const string DefaultContentKind = "text";
    private const string TriggerButtonContentKind = "button-trigger";

    private int gridX;
    private int gridY;
    private int gridWidth = DefaultGridWidth;
    private int gridHeight = DefaultGridHeight;
    private string? sourceFlowId;
    private string? sourcePortId;
    private string settingsJson = "{}";
    private string title;
    private string contentKind = DefaultContentKind;
    private string content = string.Empty;
    private string displayDataJson = "{}";
    private bool isTextWrapEnabled = true;
    private bool isSourceNodeEnabled = true;
    private bool supportsAutoScroll;
    private bool supportsAutoScrollFallback;
    private bool isAutoScrollEnabled = true;
    private bool isInteractionAdornerVisible;

    public DashboardWidgetViewModel(
        Guid id,
        string title,
        string widgetType,
        int gridX,
        int gridY,
        int gridWidth,
        int gridHeight,
        string? sourceFlowId = null,
        string? sourcePortId = null,
        string settingsJson = "{}")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(widgetType);
        ArgumentException.ThrowIfNullOrWhiteSpace(settingsJson);

        Id = id;
        this.title = title;
        WidgetType = widgetType;
        this.gridX = Math.Max(0, gridX);
        this.gridY = Math.Max(0, gridY);
        this.gridWidth = Math.Max(1, gridWidth);
        this.gridHeight = Math.Max(1, gridHeight);
        this.sourceFlowId = sourceFlowId;
        this.sourcePortId = sourcePortId;
        ApplySettingsJson(settingsJson);
    }

    public Guid Id { get; }

    public string Title
    {
        get => title;
        private set => SetProperty(ref title, value);
    }

    public string Content
    {
        get => content;
        private set => SetProperty(ref content, value);
    }

    public string ContentKind
    {
        get => contentKind;
        private set
        {
            if (SetProperty(ref contentKind, string.IsNullOrWhiteSpace(value) ? DefaultContentKind : value))
            {
                OnPropertyChanged(nameof(IsTextContent));
                OnPropertyChanged(nameof(IsTriggerButtonContent));
                OnPropertyChanged(nameof(IsTextContentAndWrapEnabled));
                OnPropertyChanged(nameof(IsTextContentAndNoWrapEnabled));
            }
        }
    }

    public bool IsTextContent => string.Equals(ContentKind, DefaultContentKind, StringComparison.OrdinalIgnoreCase);

    public bool IsTextContentAndWrapEnabled => IsTextContent && IsTextWrapEnabled;

    public bool IsTextContentAndNoWrapEnabled => IsTextContent && IsTextNoWrapEnabled;

    public bool IsTriggerButtonContent => string.Equals(ContentKind, TriggerButtonContentKind, StringComparison.OrdinalIgnoreCase);

    public bool IsAutoScrollToggleVisible => IsTextContent;

    public string AutoScrollButtonLabel => IsAutoScrollEnabled ? "AutoScroll ON" : "AutoScroll OFF";

    public string DisplayDataJson
    {
        get => displayDataJson;
        private set => SetProperty(ref displayDataJson, string.IsNullOrWhiteSpace(value) ? "{}" : value);
    }

    public bool IsTextWrapEnabled
    {
        get => isTextWrapEnabled;
        private set
        {
            if (SetProperty(ref isTextWrapEnabled, value))
            {
                OnPropertyChanged(nameof(IsTextNoWrapEnabled));
                OnPropertyChanged(nameof(IsTextContentAndWrapEnabled));
                OnPropertyChanged(nameof(IsTextContentAndNoWrapEnabled));
            }
        }
    }

    public bool IsTextNoWrapEnabled => !IsTextWrapEnabled;

    public bool IsSourceNodeEnabled
    {
        get => isSourceNodeEnabled;
        private set
        {
            if (SetProperty(ref isSourceNodeEnabled, value))
            {
                OnPropertyChanged(nameof(HeaderBackground));
            }
        }
    }

    public bool SupportsAutoScroll
    {
        get => supportsAutoScroll;
        private set
        {
            if (SetProperty(ref supportsAutoScroll, value))
            {
                OnPropertyChanged(nameof(IsAutoScrollToggleVisible));
            }
        }
    }

    public bool SupportsAutoScrollFallback
    {
        get => supportsAutoScrollFallback;
        private set
        {
            if (SetProperty(ref supportsAutoScrollFallback, value))
            {
                OnPropertyChanged(nameof(IsAutoScrollToggleVisible));
            }
        }
    }

    public bool IsAutoScrollEnabled
    {
        get => isAutoScrollEnabled;
        private set
        {
            if (SetProperty(ref isAutoScrollEnabled, value))
            {
                OnPropertyChanged(nameof(AutoScrollButtonLabel));
            }
        }
    }

    public string HeaderBackground => IsSourceNodeEnabled ? "#2B4D87" : "#94A3B8";

    public bool IsInteractionAdornerVisible
    {
        get => isInteractionAdornerVisible;
        set => SetProperty(ref isInteractionAdornerVisible, value);
    }

    public string WidgetType { get; }

    public string? SourceFlowId
    {
        get => sourceFlowId;
        set => SetProperty(ref sourceFlowId, value);
    }

    public string? SourcePortId
    {
        get => sourcePortId;
        set => SetProperty(ref sourcePortId, value);
    }

    public string SettingsJson
    {
        get => settingsJson;
        set
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            if (SetProperty(ref settingsJson, value))
            {
                ApplyWidgetSettings(value);
            }
        }
    }

    public int GridX
    {
        get => gridX;
        set
        {
            if (SetProperty(ref gridX, Math.Max(0, value)))
            {
                OnPropertyChanged(nameof(PixelX));
            }
        }
    }

    public int GridY
    {
        get => gridY;
        set
        {
            if (SetProperty(ref gridY, Math.Max(0, value)))
            {
                OnPropertyChanged(nameof(PixelY));
            }
        }
    }

    public int GridWidth
    {
        get => gridWidth;
        set
        {
            if (SetProperty(ref gridWidth, Math.Max(1, value)))
            {
                OnPropertyChanged(nameof(PixelWidth));
            }
        }
    }

    public int GridHeight
    {
        get => gridHeight;
        set
        {
            if (SetProperty(ref gridHeight, Math.Max(1, value)))
            {
                OnPropertyChanged(nameof(PixelHeight));
            }
        }
    }

    public double PixelX => GridX * DashboardViewModel.GridSizePixels;

    public double PixelY => GridY * DashboardViewModel.GridSizePixels;

    public double PixelWidth => GridWidth * DashboardViewModel.GridSizePixels;

    public double PixelHeight => GridHeight * DashboardViewModel.GridSizePixels;

    public void UpdateFromDocument(
        Guid id,
        string title,
        int gridX,
        int gridY,
        int gridWidth,
        int gridHeight,
        string? sourceFlowId,
        string? sourcePortId,
        string settingsJson)
    {
        if (Id != id)
        {
            throw new ArgumentException("The widget identifier cannot be changed.", nameof(id));
        }

        Title = title;
        GridX = gridX;
        GridY = gridY;
        GridWidth = gridWidth;
        GridHeight = gridHeight;
        SourceFlowId = sourceFlowId;
        SourcePortId = sourcePortId;
        SettingsJson = settingsJson;
    }

    public void ToggleAutoScroll()
    {
        var updatedIsAutoScrollEnabled = !IsAutoScrollEnabled;
        var settings = ReadSettingsObject();
        settings["supportsAutoScroll"] = true;
        settings["isAutoScrollEnabled"] = updatedIsAutoScrollEnabled;
        SettingsJson = settings.ToJsonString();
    }

    private void ApplySettingsJson(string value)
    {
        settingsJson = value;
        ApplyWidgetSettings(value);
    }

    private void ApplyWidgetSettings(string value)
    {
        var parsedTitle = Title;
        var parsedContentKind = DefaultContentKind;
        var parsedContent = string.Empty;
        var parsedDisplayDataJson = "{}";
        var parsedIsTextWrapEnabled = true;
        var parsedIsEnabled = true;
        var parsedSupportsAutoScroll = false;
        var parsedSupportsAutoScrollFallback = false;
        var parsedIsAutoScrollEnabled = true;

        using var document = JsonDocument.Parse(value);
        if (document.RootElement.ValueKind == JsonValueKind.Object)
        {
            if (document.RootElement.TryGetProperty("title", out var titleElement)
                && titleElement.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(titleElement.GetString()))
            {
                parsedTitle = titleElement.GetString()!;
            }

            if (document.RootElement.TryGetProperty("contentKind", out var contentKindElement)
                && contentKindElement.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(contentKindElement.GetString()))
            {
                parsedContentKind = contentKindElement.GetString()!;
            }

            if (document.RootElement.TryGetProperty("content", out var contentElement)
                && contentElement.ValueKind == JsonValueKind.String)
            {
                parsedContent = contentElement.GetString() ?? string.Empty;
            }

            if (document.RootElement.TryGetProperty("displayData", out var displayDataElement))
            {
                parsedDisplayDataJson = displayDataElement.GetRawText();
                if (string.IsNullOrEmpty(parsedContent)
                    && displayDataElement.ValueKind == JsonValueKind.Object
                    && displayDataElement.TryGetProperty("text", out var textElement)
                    && textElement.ValueKind == JsonValueKind.String)
                {
                    parsedContent = textElement.GetString() ?? string.Empty;
                }
            }

            if (document.RootElement.TryGetProperty("isSourceNodeEnabled", out var enabledElement)
                && enabledElement.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                parsedIsEnabled = enabledElement.GetBoolean();
            }

            if (document.RootElement.TryGetProperty("isTextWrapEnabled", out var textWrapElement)
                && textWrapElement.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                parsedIsTextWrapEnabled = textWrapElement.GetBoolean();
            }

            if (document.RootElement.TryGetProperty("supportsAutoScroll", out var supportsAutoScrollElement)
                && supportsAutoScrollElement.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                parsedSupportsAutoScroll = supportsAutoScrollElement.GetBoolean();
            }

            if (document.RootElement.TryGetProperty("isAutoScrollEnabled", out var autoScrollEnabledElement)
                && autoScrollEnabledElement.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                parsedIsAutoScrollEnabled = autoScrollEnabledElement.GetBoolean();
            }

            if (!parsedSupportsAutoScroll)
            {
                parsedSupportsAutoScrollFallback =
                    string.Equals(parsedTitle, "StremOutputTS", StringComparison.Ordinal)
                    || string.Equals(parsedTitle, "StreamOutputTS", StringComparison.Ordinal)
                    || parsedContent.StartsWith("millis,value", StringComparison.Ordinal);
            }
        }

        Title = parsedTitle;
        ContentKind = parsedContentKind;
        Content = parsedContent;
        DisplayDataJson = parsedDisplayDataJson;
        IsTextWrapEnabled = parsedIsTextWrapEnabled;
        IsSourceNodeEnabled = parsedIsEnabled;
        SupportsAutoScroll = parsedSupportsAutoScroll;
        SupportsAutoScrollFallback = parsedSupportsAutoScrollFallback;
        IsAutoScrollEnabled = parsedIsAutoScrollEnabled;
    }

    private JsonObject ReadSettingsObject()
    {
        try
        {
            return JsonNode.Parse(SettingsJson) as JsonObject ?? new JsonObject();
        }
        catch (JsonException)
        {
            return new JsonObject();
        }
    }
}
