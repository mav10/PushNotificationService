using System;
using System.Linq.Expressions;
using MccSoft.PushNotification.App.Features.MobileUsers.Dto;
using MccSoft.PushNotification.Domain;
using NeinLinq;

namespace MccSoft.PushNotification.App.Features.MobileUsers
{
    public static class UserExtensions
    {
        [InjectLambda]
        public static UserDto ToUserDto(this User record)
        {
            return ToUserDtoExpressionCompiled.Value(record);
        }

        private static readonly Lazy<Func<User, UserDto>> ToUserDtoExpressionCompiled =
            new(() => ToUserDto().Compile());

        public static Expression<Func<User, UserDto>> ToUserDto() =>
            record =>
                new UserDto()
                {
                    Id = record.Id,
                    FullName = $"{record.LastName} {record.FirstName}",
                    LastActivityAt = record.LastVisitDateTime,
                    IsCodeGenerated = record.PasswordHash != null
                };
    }
}