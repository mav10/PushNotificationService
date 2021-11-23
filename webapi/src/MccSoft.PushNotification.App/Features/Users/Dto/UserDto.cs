using System;

namespace MccSoft.PushNotification.App.Features.Users.Dto
{
    public class UserDto
    {
        /// <summary>
        /// User's Id.
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// User's first and last name.
        /// </summary>
        public string FullName { get; set; }
        
        /// <summary>
        /// Date when user the last time used the app
        /// <value>NULL</value> - if user has never been logged in in the app. 
        /// </summary>
        public DateTime? LastActivityAt { get; set; }
        
        /// <summary>
        /// Flag indicates that auth code was generated for this user or does not.
        /// <value>NULL</value> - if user has never been logged in in the app. 
        /// </summary>
        public bool IsCodeGenerated { get; set; }
    }
}