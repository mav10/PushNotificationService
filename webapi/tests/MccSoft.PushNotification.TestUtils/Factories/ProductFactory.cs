using MccSoft.PushNotification.App.Features.Products.Dto;
using MccSoft.PushNotification.Domain;
using MccSoft.Testing.Infrastructure;

namespace MccSoft.PushNotification.TestUtils.Factories
{
    public static class ProductFactory
    {
        public static Product Product(this MotherFactory a, string title = "Default Product 1")
        {
            return new Product(title);
        }

        public static Http.Generated.CreateProductDto CreateProductGeneratedDto(
            this MotherFactory a,
            string title = "Default Product 1"
        ) {
            return new() { Title = title, };
        }

        public static CreateProductDto CreateProductDto(
            this MotherFactory a,
            string title = "Default Product 1"
        ) {
            return new() { Title = title, };
        }
    }
}
