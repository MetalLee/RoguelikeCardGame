namespace RoguelikeCardGame.Presentation.Battle;

public static class BeatSlotPresentationGeometry
{
    private static readonly string[] RomanNumerals =
    [
        "I",
        "II",
        "III",
        "IV",
        "V",
        "VI",
        "VII",
        "VIII",
        "IX",
        "X",
        "XI",
        "XII"
    ];

    public static string RomanBeatNumber(int beatIndex)
    {
        var value = beatIndex + 1;
        return value >= 1 && value <= RomanNumerals.Length
            ? RomanNumerals[value - 1]
            : value.ToString();
    }

    public static bool IsPointInsideDiamond(float localX, float localY, float width, float height)
    {
        if (width <= 0f || height <= 0f)
        {
            return false;
        }

        var halfWidth = width * 0.5f;
        var halfHeight = height * 0.5f;
        var normalizedDistance =
            Math.Abs(localX - halfWidth) / halfWidth +
            Math.Abs(localY - halfHeight) / halfHeight;
        return normalizedDistance <= 1f;
    }

    public static BeatConnectorLine InsetConnectorLine(float startX, float startY, float endX, float endY, float inset)
    {
        var deltaX = endX - startX;
        var deltaY = endY - startY;
        var length = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        if (length <= 0d)
        {
            return new BeatConnectorLine(startX, startY, endX, endY);
        }

        var clampedInset = MathF.Max(0f, MathF.Min(inset, (float)length * 0.45f));
        var directionX = (float)(deltaX / length);
        var directionY = (float)(deltaY / length);
        return new BeatConnectorLine(
            startX + directionX * clampedInset,
            startY + directionY * clampedInset,
            endX - directionX * clampedInset,
            endY - directionY * clampedInset);
    }

    public static float RaisedConnectorControlY(float startY, float endY, float lift)
    {
        return MathF.Min(startY, endY) - MathF.Max(0f, lift);
    }

    public static BeatArrowBaseCenters ArrowBaseCenter(float startX, float startY, float endX, float endY, float arrowLength)
    {
        var line = InsetConnectorLine(startX, startY, endX, endY, arrowLength);
        return new BeatArrowBaseCenters(line.StartX, line.StartY, line.EndX, line.EndY);
    }

    public static BeatPoint ArrowTipFromBaseCenter(float baseX, float baseY, float tangentX, float tangentY, float arrowLength)
    {
        var length = Math.Sqrt(tangentX * tangentX + tangentY * tangentY);
        if (length <= 0d)
        {
            return new BeatPoint(baseX, baseY);
        }

        return new BeatPoint(
            baseX + (float)(tangentX / length) * arrowLength,
            baseY + (float)(tangentY / length) * arrowLength);
    }

    public static BeatPoint ArrowTailFromTip(float tipX, float tipY, float tangentTowardTipX, float tangentTowardTipY, float arrowLength)
    {
        var length = Math.Sqrt(tangentTowardTipX * tangentTowardTipX + tangentTowardTipY * tangentTowardTipY);
        if (length <= 0d)
        {
            return new BeatPoint(tipX, tipY);
        }

        return new BeatPoint(
            tipX - (float)(tangentTowardTipX / length) * arrowLength,
            tipY - (float)(tangentTowardTipY / length) * arrowLength);
    }
}

public readonly record struct BeatConnectorLine(float StartX, float StartY, float EndX, float EndY);

public readonly record struct BeatArrowBaseCenters(float SourceBaseX, float SourceBaseY, float TargetBaseX, float TargetBaseY);

public readonly record struct BeatPoint(float X, float Y);
