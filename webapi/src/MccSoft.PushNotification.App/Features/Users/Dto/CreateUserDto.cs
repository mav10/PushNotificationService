using System.ComponentModel.DataAnnotations;

namespace MccSoft.PushNotification.App.Features.Users.Dto
{
    public class CreateUserDto
    {
        
        /// <summary>
        /// User's login
        /// </summary>
        [Required]
        public string Login { get; set; }

        /// <summary>
        /// User's First name
        /// </summary>
        [Required]
        [MaxLength(128)]
        public string FirstName { get; set; }
        
        /// <summary>
        /// User's last name.
        /// </summary>
        [MaxLength(128)]
        public string LastName { get; set; }
    }
}