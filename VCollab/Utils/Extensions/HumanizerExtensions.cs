
namespace VCollab.Utils.Extensions;

public static class HumanizerExtensions
{
    public static string Bytes(this double value)
    {
        // First convert to bits
        value *= 8d;

        // Decide on unit depending on value
        return value switch
        {
            < 1_000d => $"{value:#.##} bit",
            < 1_000_000d => $"{(value / 1_000d):#.##} Kbit",
            < 1_000_000_000d => $"{(value / 1_000_000d):#.##} Mbit",
            _ => $"{(value / 1_000_000_000d):#.##} Gbit"
        };
    }
}