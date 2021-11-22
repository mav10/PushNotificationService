using System.Threading.Tasks;
using MccSoft.PushNotification.App.Features.MobileUsers.Dto;
using MccSoft.PushNotification.App.Features.Products.Dto;
using MccSoft.WebApi.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MccSoft.PushNotification.App.Features.MobileUsers
{
    [ApiController]
    [Authorize]
    [Route("api/users")]
    public class UserController
    {
        private readonly UserService _userService;

        public UserController(UserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Gets user by id.
        /// </summary>
        /// <param name="id">Id of teh expected user.</param>
        /// <returns>Returns user by Id.</returns>
        [HttpGet("{id}")]
        public async Task<UserDto> GetUser(string id)
        {
            return await _userService.GetUserById(id);
        }

        /// <summary>
        /// Gets list of users.
        /// </summary>
        /// <param name="dto">Search query.</param>
        /// <returns>Returns list of users satisfied by filter and search query.</returns>
        [HttpGet]
        public async Task<PagedResult<UserDto>> GetAllUsers([FromQuery] SearchUserDto dto)
        {
            
            return await _userService.GetUsers(dto);
        }

        /// <summary>
        /// Creates new user with given params.
        /// </summary>
        /// <param name="dto">Create user DTO.</param>
        /// <returns>Return newly created user.</returns>
        [HttpPost]
        public async Task<UserDto> CreateUser([FromBody] CreateUserDto dto)
        {
            return await _userService.CreateUser(dto);
        }
    }
}