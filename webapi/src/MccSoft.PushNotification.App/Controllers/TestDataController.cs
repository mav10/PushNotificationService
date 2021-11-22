﻿using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Threading.Tasks;
using MccSoft.Mailing;
using MccSoft.PushNotification.App.Views.Emails.Example;
using MccSoft.WebApi.Domain.Helpers;
using MccSoft.WebApi.Patching.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace MccSoft.PushNotification.App.Controllers
{
    /// <summary>
    /// Contains some actions to test the basic things
    /// </summary>
    [ApiController]
    public class TestDataController
    {
        /// <summary>
        /// Demonstrates an error response.
        /// </summary>
        [AllowAnonymous]
        [HttpGet]
        [Route("error-test")]
        public string ThrowError()
        {
            throw new ArgumentException("Invalid arg.", "argName");
        }

        /// <summary>
        /// Sends a dummy email
        /// </summary>
        [AllowAnonymous]
        [HttpPost]
        [Route("send-email")]
        public async Task<string> SendEmail([FromServices] IMailSender mailSender)
        {
            var model = new ExampleEmailModel() { Username = "Vasiliy Pupkin" };
            await mailSender.Send("mcc.template.app@gmail.com", model);

            return "OK";
        }

        public class TestPatchDto : PatchRequest<object>
        {
            [DoNotPatch]
            [RequiredOrMissing]
            public string Value { get; set; }
        }

        /// <summary>
        /// Tests RequiredOrUndefined attribute
        /// </summary>
        [AllowAnonymous]
        [HttpPost]
        [Route("patch")]
        public string Patch(TestPatchDto dto)
        {
            return JsonConvert.SerializeObject(dto);
        }
    }
}
