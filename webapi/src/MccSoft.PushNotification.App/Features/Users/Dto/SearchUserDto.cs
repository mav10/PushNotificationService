using MccSoft.WebApi.Pagination;

namespace MccSoft.PushNotification.App.Features.Users.Dto
{
    public class SearchUserDto : PagedRequestDto
    {
        public string? Search { get; set; }
    }
}
