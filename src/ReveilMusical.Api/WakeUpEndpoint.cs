using ReveilMusical.Application;

namespace ReveilMusical.Api;

internal static class WakeUpEndpoint
{
    public static async Task<IResult> HandleAsync(WakeUpRequestDto body, WakeUpService service, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(body.UserId))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["userId"] = ["userId is required."] });
        }

        var result = await service.WakeUpAsync(new WakeUpRequest(body.UserId, body.Day, body.Weather), cancellationToken);

        return result.Status switch
        {
            WakeUpStatus.UserNotFound => TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Unknown user",
                detail: $"No user with id '{body.UserId}'."),
            WakeUpStatus.NotDelivered => TypedResults.Json(WakeUpResponse.From(result), statusCode: StatusCodes.Status503ServiceUnavailable),
            _ => TypedResults.Ok(WakeUpResponse.From(result)),
        };
    }
}
