using ReveilMusical.Domain;

namespace ReveilMusical.Application;

public enum WakeUpStatus
{
    Delivered,
    NotDelivered,
    UserNotFound,
}

public enum DegradationReason
{
    ProfileUnavailable,
    MusicFallbackUsed,
    ChannelFallbackUsed,
    NotDelivered,
}

public sealed record WakeUpResult(
    WakeUpStatus Status,
    Track? Track,
    ChannelType? DeliveredVia,
    IReadOnlyList<DegradationReason> Reasons)
{
    public static WakeUpResult UserNotFound { get; } = new(WakeUpStatus.UserNotFound, null, null, []);

    public bool IsDegraded => Reasons.Count > 0;
}
