using MccSoft.PushNotification.Domain;

namespace MccSoft.PushNotification.App.Features.Products.Dto
{
    public class ProductListItemDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public ProductType ProductType { get; set; }

        public ProductListItemDto() { }
    }
}
