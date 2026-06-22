namespace RoguelikeCardGame.Presentation.Battle;

public static class BeatClashOverlayPresentation
{
    public const float TargetGapPixels = 100f;
    public const float MoveTolerancePixels = 4f;

    public static BeatClashOverlayPoint DashPosition(
        BeatClashOverlayFrame playerPosition,
        BeatClashOverlayFrame targetPosition)
    {
        return new BeatClashOverlayPoint(
            targetPosition.X - playerPosition.Width - TargetGapPixels,
            playerPosition.Y);
    }

    public static IReadOnlyList<BeatClashOverlayHealthFrame> HealthAfterStages(
        int currentHp,
        int maxHp,
        IReadOnlyList<BeatClashActionStage> stages)
    {
        var remainingHp = Math.Clamp(currentHp, 0, Math.Max(0, maxHp));
        var frames = new List<BeatClashOverlayHealthFrame>();
        foreach (var stage in stages)
        {
            remainingHp = Math.Max(0, remainingHp - Math.Max(0, stage.EnemyDamage));
            frames.Add(new BeatClashOverlayHealthFrame(
                remainingHp,
                maxHp <= 0 ? 0f : Math.Clamp((float)remainingHp / maxHp, 0f, 1f)));
        }

        return frames;
    }

    public static bool ShouldMove(BeatClashOverlayPoint currentPosition, BeatClashOverlayPoint targetPosition)
    {
        var dx = targetPosition.X - currentPosition.X;
        var dy = targetPosition.Y - currentPosition.Y;
        return dx * dx + dy * dy > MoveTolerancePixels * MoveTolerancePixels;
    }

    public static bool ShouldPlayDash(
        BeatClashOverlayPoint currentPosition,
        BeatClashOverlayPoint targetPosition,
        bool continuesPreviousTarget)
    {
        return !continuesPreviousTarget && ShouldMove(currentPosition, targetPosition);
    }

    public static bool ShouldFaceLeft(BeatClashOverlayPoint currentPosition, BeatClashOverlayPoint targetPosition)
    {
        return targetPosition.X < currentPosition.X;
    }
}

public readonly record struct BeatClashOverlayFrame(float X, float Y, float Width, float Height);

public readonly record struct BeatClashOverlayPoint(float X, float Y);

public readonly record struct BeatClashOverlayHealthFrame(int CurrentHp, float HealthRatio);
