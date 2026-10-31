using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FitTrack.Infrastructure.Persistence;

public class SnakeCaseEnumConverter<TEnum>() : ValueConverter<TEnum, string>(
    value => value.ToValue(),
    text => EnumValues.Parse<TEnum>(text))
    where TEnum : struct, Enum;
