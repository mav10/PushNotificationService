using System.Threading.Tasks;
using FluentAssertions;
using MccSoft.PushNotification.App.Features.Products;
using MccSoft.PushNotification.App.Utils;
using MccSoft.PushNotification.TestUtils.Factories;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MccSoft.PushNotification.App.Tests
{
    public class ProductServiceTests : AppServiceTestBase<ProductService>
    {
        private readonly DateTimeProvider _time = new DateTimeProvider();

        public ProductServiceTests()
        {
            var logger = new NullLogger<ProductService>();

            Sut = InitializeService((retryHelper, db) => new ProductService(db));
        }

        [Fact]
        public async Task Create()
        {
            var result = await Sut.Create(a.CreateProductDto("asd"));
            result.Title.Should().Be("asd");
        }
    }
}
