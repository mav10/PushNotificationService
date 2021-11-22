using System;
using MccSoft.Testing;
using Xunit;

namespace MccSoft.PushNotification.Domain.Tests
{
    public class UserTests : TestBase
    {
        [Fact]
        public void Ctor_Created()
        {
            var name = "Jhon";
            var lastName = "Doe";
            var email = "test@com.de";
            var time = new DateTime(2022, 1, 1);
            
            var user = new User(name, lastName, email, time);
            
            Assert.Equal(name, user.FirstName);
            Assert.Equal(lastName, user.LastName);
            Assert.Equal(email, user.Email);
            Assert.Equal(time, user.CreatedAt);
        }
    }
}