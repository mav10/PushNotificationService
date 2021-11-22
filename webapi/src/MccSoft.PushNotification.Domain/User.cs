using System;
using MccSoft.DomainHelpers;
using Microsoft.AspNetCore.Identity;

namespace MccSoft.PushNotification.Domain
{
    public class User : IdentityUser
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }

        /// <summary>
        /// Date and time when user was created. (server UTC time).
        /// </summary>
        public DateTime CreatedAt { get; private set; }

        /// <summary>
        /// Date and time when user used the system the last time.
        /// </summary>
        public DateTime? LastVisitDateTime { get; private set; }


        /// <summary>
        /// Unique identifier of the User.  
        /// </summary>
        public int UniqueId { get; set; }

        /// <summary>
        /// Needed for Entity Framework, keep empty.
        /// </summary>
        public User()
        {
        }

        /// <summary>
        /// Constructor to initialize User entity.
        /// </summary>
        public User(string email)
        {
            UserName = email;
            Email = email;
        }

        /// <summary>
        /// Constructor to initialize User entity with first and last name.
        /// </summary>
        /// <param name="firstName">First name of the user.</param>
        /// <param name="lastName">Last name of the user.</param>
        /// <param name="email">Identifier of the user.</param>
        public User(string firstName, string lastName, string email, DateTime createdAt)
        {
            UserName = email;
            Email = email;
            FirstName = firstName;
            LastName = lastName;
            CreatedAt = createdAt;
        }

        /// <summary>
        /// Combined string from Users's fields used in search queries
        /// </summary>
        public string SearchQueryString
        {
            get =>
                $"{UniqueId.ToString()} {FirstName} {LastName}"
            ;
            private set { } // empty setter is necessary for EF Core
        }

        /// <summary>
        /// Creates a specification that is satisfied by a user having the specified id.
        /// </summary>
        /// <param name="id">The user id.</param>
        /// <returns>The created specification.</returns>
        public static Specification<User> HasId(string id) =>
            new(nameof(HasId), p => p.Id == id, id);

        public static Specification<User> HasEmail(string email) =>
            new(nameof(HasEmail), p => p.NormalizedEmail == email.ToUpper(), email);
    }
}