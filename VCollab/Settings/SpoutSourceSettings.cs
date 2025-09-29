namespace VCollab.Settings;

public record SpoutSourceSettings(
    string SenderName,

    float OffsetX,
    float OffsetY,

    int TextureWidth,
    int TextureHeight,
    float RelativeWidth,
    float RelativeHeight,

    bool ShowInOutput
);