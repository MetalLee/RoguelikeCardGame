using System;

namespace RoguelikeCardGame.Presentation.Battle;

public static class BeatTargetingInputPresentation
{
    public static void CompleteTargetSelection(
        Action markInputHandled,
        Action hideArrow,
        Action clearTargetingState,
        Action publishSelection)
    {
        markInputHandled();
        hideArrow();
        clearTargetingState();
        publishSelection();
    }
}
