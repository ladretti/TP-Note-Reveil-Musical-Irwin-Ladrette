using Microsoft.Extensions.Logging;
using ReveilMusical.Domain;
using ReveilMusical.Domain.Ports;

namespace ReveilMusical.Application;

public sealed class WakeUpService(
    IUserProfileProvider profiles,
    ITrackResolver tracks,
    IWakeUpNotifier notifier,
    ILogger<WakeUpService> logger)
{
    public async Task<WakeUpResult> WakeUpAsync(WakeUpRequest request, CancellationToken cancellationToken)
    {
        var reasons = new List<DegradationReason>();

        UserProfile? profile;
        try
        {
            profile = await profiles.GetAsync(request.UserId, cancellationToken);
            if (profile is null)
            {
                return WakeUpResult.UserNotFound;
            }
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Profile service unavailable for user {UserId}, waking up in degraded mode", request.UserId);
            reasons.Add(DegradationReason.ProfileUnavailable);
            profile = null;
        }

        var query = new TrackQuery(profile?.SongFor(request.Day, request.Weather), request.Weather);
        var resolved = await tracks.ResolveAsync(query, cancellationToken);
        if (resolved.IsFallback)
        {
            reasons.Add(DegradationReason.MusicFallbackUsed);
        }

        var message = WakeUpMessage.For(request.UserId, profile?.Contact ?? UserContact.None, request.Day, request.Weather, resolved.Track);
        var notification = await notifier.NotifyAsync(message, profile?.PreferredChannel, cancellationToken);

        if (!notification.Delivered)
        {
            reasons.Add(DegradationReason.NotDelivered);
            logger.LogCritical("Wake-up of user {UserId} could not be delivered on any channel", request.UserId);
            return new WakeUpResult(WakeUpStatus.NotDelivered, resolved.Track, null, reasons);
        }

        if (notification.UsedFallback)
        {
            reasons.Add(DegradationReason.ChannelFallbackUsed);
        }

        return new WakeUpResult(WakeUpStatus.Delivered, resolved.Track, notification.DeliveredVia, reasons);
    }
}
