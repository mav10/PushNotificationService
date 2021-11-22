using System.ComponentModel.DataAnnotations;
using MccSoft.PushNotification.Domain;
using MccSoft.WebApi.Patching.Models;

namespace MccSoft.PushNotification.App.Features.Products.Dto
{
    public class PatchProductDto : PatchRequest<Product>
    {
        [MinLength(3)]
        public string Title { get; set; }
        public ProductType ProductType { get; set; }
    }
}
