using MccSoft.PushNotification.Domain;
using MccSoft.WebApi.Pagination;

namespace MccSoft.PushNotification.App.Features.Products.Dto
{
    public class SearchUserDto : PagedRequestDto
    {
        public string? Search { get; set; }
    }
}
