using System;

public enum ProgressMetricType
{
    EarnCoin,
    OpenCrate,
    CompleteFactoryOffer,
    GainResearchPoint,
    GainLevel,
    UnlockSlot,
    UnlockCropType,
    MergeCrop
}

public static class ProgressionSignals
{
    public static event Action<ProgressMetricType, int> OnMetricProgress;

    public static void Raise(ProgressMetricType metric, int amount)
    {
        if (amount <= 0)
            return;

        OnMetricProgress?.Invoke(metric, amount);
    }
}
