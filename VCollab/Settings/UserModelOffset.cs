namespace VCollab.Settings;

public class UserModelSettings
{
    public SaveableVector2 PositionOffset { get; set; } = new(0f, 0f);
    public float Scale { get; set; } = 1f;
}