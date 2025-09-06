namespace VCollab.Settings;

public class SaveableVector2
{
    public float X { get; set; }
    public float Y { get; set; }

    public SaveableVector2(float x, float y)
    {
        X = x;
        Y = y;
    }

    public static implicit operator SaveableVector2(Vector2 vector2) => new(vector2.X, vector2.Y);

    public static implicit operator Vector2(SaveableVector2 saveableVector2) => new(saveableVector2.X, saveableVector2.Y);
}