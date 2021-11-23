using System.Threading.Tasks;
using MccSoft.PushNotification.App.Features.Users.Dto;
using MccSoft.PushNotification.App.Utils;
using MccSoft.WebApi.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MccSoft.PushNotification.App.Features.Users
{
    [Authorize]
    [Route("api/users")]
    public class UserController : ControllerBase
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
        /// <response code="201">Returns the newly created user DTO.</response>
        /// <response code="400">Invalid data was specified.</response>
        /// <returns>Return newly created user.</returns>
        [HttpPost]
        [ProducesResponseType(201)]
        [ProducesResponseType(400, Type = typeof(ValidationProblemDetails))]
        public async Task<CreatedAtActionResult> CreateUser([FromBody] CreateUserDto dto)
        {
            var userDto = await _userService.CreateUser(User.GetUserId(), dto);
            return CreatedAtAction(nameof(GetUser), new { id = userDto.Id }, userDto);
        }

        /// <summary>
        /// Generates authentication code for mobile user.
        /// </summary>
        /// <returns>Returns auth code.</returns>
        [HttpPost("code")]
        public async Task<string> GenerateUserAccessCode(string userId)
        {
            return await _userService.GenerateCode(User.GetUserId(), userId);
        }
    }
}