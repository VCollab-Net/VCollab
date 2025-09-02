namespace VCollab.Settings;

public record SpoutSourceSettings(
    string SenderName,

    double OffsetX,
    double OffsetY,

    double RelativeWidth,
    double RelativeHeight
);