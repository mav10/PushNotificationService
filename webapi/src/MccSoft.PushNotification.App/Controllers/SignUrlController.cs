using MccSoft.WebApi.SignedUrl;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MccSoft.PushNotification.App.Controllers
{
    [ApiController]
    [Route("api/sign-url")]
    public class SignUrlController
    {
        [HttpGet("signature")]
        [Authorize]
        public string GetSignature([FromServices] SignUrlHelper signUrlHelper)
        {
            return signUrlHelper.GenerateSignature();
        }

        [HttpGet("signature/cookie")]
        [Authorize]
        public ActionResult SetSignatureCookie(
            [FromServices] SignUrlHelper signUrlHelper,
            [FromServices] IHttpContextAccessor httpContextAccessor
        ) {
            string signature = signUrlHelper.GenerateSignature();

            httpContextAccessor.HttpContext!.Response.Cookies.Append(
                SignUrlHelper.UrlParameterName,
                signature
            );
            return new OkResult();
        }
    }
}
