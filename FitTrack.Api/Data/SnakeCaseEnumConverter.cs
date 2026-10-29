using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FitTrack.Api.Data;

public class SnakeCaseEnumConverter<TEnum>() : ValueConverter<TEnum, string>(
    value => value.ToValue(),
    text => EnumValues.Parse<TEnum>(text))
    where TEnum : struct, Enum;
