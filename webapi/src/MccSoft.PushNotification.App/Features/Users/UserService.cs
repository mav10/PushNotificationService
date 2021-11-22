using System.Linq;
using System.Threading.Tasks;
using MccSoft.Logging;
using MccSoft.LowLevelPrimitives.Exceptions;
using MccSoft.PersistenceHelpers;
using MccSoft.PushNotification.App.Features.MobileUsers.Dto;
using MccSoft.PushNotification.App.Features.Products;
using MccSoft.PushNotification.App.Features.Products.Dto;
using MccSoft.PushNotification.App.Utils;
using MccSoft.PushNotification.Domain;
using MccSoft.PushNotification.Persistence;
using MccSoft.WebApi.Pagination;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace MccSoft.PushNotification.App.Features.MobileUsers
{
    public class UserService
    {
        /// <summary>
        /// use only for read operations. For data manipulation use transaction based solution <see cref="_retryHelper"/>.
        /// </summary>
        private readonly PushNotificationDbContext _readOnlyContext;

        private UserManager<User> _userManager;
        private readonly IDateTimeProvider _timeProvider;
        private readonly ILogger<UserService> _logger;

        public UserService(PushNotificationDbContext readOnlyContext,
            IDateTimeProvider timeProvider, ILogger<UserService> logger, UserManager<User> userManager)
        {
            _readOnlyContext = readOnlyContext;
            _timeProvider = timeProvider;
            _logger = logger;
            _userManager = userManager;
        }

        /// <summary>
        /// Find user by given Id and return it.
        /// </summary>
        /// <param name="userId">User Id.</param>
        /// <returns>Found user by id.</returns>
        public async Task<UserDto> GetUserById(string userId)
        {
            var user = await _readOnlyContext.Users.GetOne(User.HasId(userId));
            return user.ToUserDto();
        }

        /// <summary>
        /// Load all users from DB and send it through the API.
        /// </summary>
        /// <returns>Returns all users except "admin"</returns>
        public async Task<PagedResult<UserDto>> GetUsers(SearchUserDto dto)
        {
            return await _logger.LogOperation(new OperationContext()
            {
                { Field.Named(nameof(dto.Offset)), dto.Offset },
                { Field.Named(nameof(dto.Limit)), dto.Limit },
                { Field.Named(nameof(dto.SortBy)), dto.SortBy },
                { Field.Named(nameof(dto.SortOrder)), dto.SortOrder },
            }, async () =>
            {
                IQueryable<User> query = _readOnlyContext.Users;

                if (!string.IsNullOrEmpty(dto.Search))
                {
                    query = query.Where(x => x.SearchQueryString.Contains(dto.Search));
                }

                return await query
                    .Select(x => x.ToUserDto())
                    .ToPagingListAsync(dto, nameof(User.Id));
            });
        }

        /// <summary>
        /// Creates new user with login and password
        /// </summary>
        /// <returns>Returns created user.</returns>
        public async Task<UserDto> CreateUser(CreateUserDto dto)
        {
            return await _logger.LogOperation(new OperationContext()
            {
                { Field.Named(nameof(dto.Login)), dto.Login }
            }, async () =>
            {
                var email = $"{dto.FirstName[0]}.{dto.LastName}@push.test";
                var newUser = new User(dto.FirstName, dto.LastName, email, _timeProvider.UtcNow);
                var user = await _userManager.CreateAsync(newUser);

                if (user.Succeeded)
                {
                    var createdUser = await _userManager.FindByEmailAsync(email);
                    return createdUser.ToUserDto();
                }

                throw new ValidationException("Impossible to create user");
            });
        }
    }
}