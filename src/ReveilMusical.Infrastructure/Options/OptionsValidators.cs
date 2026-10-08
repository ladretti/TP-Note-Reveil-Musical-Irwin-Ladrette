using Microsoft.Extensions.Options;

namespace ReveilMusical.Infrastructure.Options;

[OptionsValidator]
internal sealed partial class MusicOptionsValidator : IValidateOptions<MusicOptions>;

[OptionsValidator]
internal sealed partial class NotificationOptionsValidator : IValidateOptions<NotificationOptions>;

[OptionsValidator]
internal sealed partial class ProfileOptionsValidator : IValidateOptions<ProfileOptions>;
