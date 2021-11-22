using MccSoft.PushNotification.Domain;
using MccSoft.WebApi.Pagination;

namespace MccSoft.PushNotification.App.Features.Products.Dto
{
    public class SearchProductDto : PagedRequestDto
    {
        public string? Search { get; set; }
        public ProductType? ProductType { get; set; }
    }
}
