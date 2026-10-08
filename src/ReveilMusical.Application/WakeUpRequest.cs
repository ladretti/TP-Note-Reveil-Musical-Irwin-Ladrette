using ReveilMusical.Domain;

namespace ReveilMusical.Application;

public sealed record WakeUpRequest(string UserId, DayOfWeek Day, Weather Weather);
