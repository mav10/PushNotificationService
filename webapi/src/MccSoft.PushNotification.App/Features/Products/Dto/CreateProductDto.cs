using System.ComponentModel.DataAnnotations;
using MccSoft.PushNotification.Domain;

namespace MccSoft.PushNotification.App.Features.Products.Dto
{
    public class CreateProductDto
    {
        [Required]
        [MinLength(3)]
        public string Title { get; set; }

        public ProductType ProductType { get; set; }
    }
}
