using System.Threading.Tasks;
using CanvasIdentity.Exceptions;
using Microsoft.AspNetCore.Http;
//using LtiLibrary.AspNetCore.Extensions;
using System.Linq;
using System.Net;
using System.Security.Claims;
using CanvasIdentity.Helpers;
using CanvasIdentity.LtiLibrary.AspNetCore.Extensions;
using CanvasIdentity.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging;

namespace CanvasIdentity.Middleware
{
    // ReSharper disable once ClassNeverInstantiated.Global
    internal class CanvasLtiCourseIdentityMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly CanvasIdentityOption _canvasIdentityOption;
        private readonly ILogger _log;

        public CanvasLtiCourseIdentityMiddleware(RequestDelegate next, CanvasIdentityOption canvasIdentityOption, ILogger<CanvasLtiCourseIdentityMiddleware> log)
        {
            _next = next;
            _canvasIdentityOption = canvasIdentityOption;
            _log = log;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Path == _canvasIdentityOption.LtiCourseLoginPath)
            {
                _log.LogDebug($"Start Lti Login");
                string session = await LtiLogin(context, _canvasIdentityOption.ConsumerSecret);
                CreateOrRestoreCourseRequestSession(context, session);
                await RedirectAfterLoginToStartPage(context, session);
            } 
            else
            {
                if(context.User.Identity.IsAuthenticated == false) 
                    throw new IdentityHttpException(HttpStatusCode.Forbidden, "User not Authenticated Cookie problem? If you use Safari turn of cross-site tracking (Settings->Privacy) or stop Ad blockers on this site");
                RestoreSessionFromPostFormOrGetQuery(context); 
                await _next.Invoke(context);
            }
        }

        private async Task RedirectAfterLoginToStartPage(HttpContext context, string session)
        {
            _log.LogInformation($"Redirect After success Login To StartPage");
            context.Response.Redirect(
                $"{_canvasIdentityOption.RedirectAfterLogin}?{_canvasIdentityOption.CanvasSessionQueryName}={session}");
            await context.Response.WriteAsync("Redirect User Login SignInAsync");
        }

        private static void CreateOrRestoreCourseRequestSession(HttpContext context, string session)
        {
            var identity = context.User.Identities.FirstOrDefault(f =>
                f.AuthenticationType == CookieAuthenticationDefaults.AuthenticationScheme);
            if (identity != null)
            {
                var removeClaim =
                    identity.Claims.FirstOrDefault(f => f.Type == LtiClaimsViewModel.ClaimName.CourseRequest);
                if (removeClaim != null)
                    identity.RemoveClaim(removeClaim);
                identity.AddClaim(new Claim(LtiClaimsViewModel.ClaimName.CourseRequest, session));
            }
        }

        /// <summary>
        /// restore session from PostForm or get Query.
        /// </summary>
        /// <param name="context"></param>
        private void RestoreSessionFromPostFormOrGetQuery(HttpContext context)
        {
            var query = context.Request.Query[_canvasIdentityOption.CanvasSessionQueryName].FirstOrDefault();
            string form = null;
            if (context.Request.HasFormContentType)
            {
                form = (string) context.Request.Form[_canvasIdentityOption.CanvasSessionQueryName] ?? "";
            }

            var identity = context.User.Identities.FirstOrDefault(f =>
                f.AuthenticationType == CookieAuthenticationDefaults.AuthenticationScheme);

            if (identity != null)
            {
                var removeClaim = identity.Claims.FirstOrDefault(f => f.Type == LtiClaimsViewModel.ClaimName.CourseRequest);
                if (removeClaim != null)
                    identity.RemoveClaim(removeClaim);
                if (!string.IsNullOrEmpty(query))
                    identity.AddClaim(new Claim(LtiClaimsViewModel.ClaimName.CourseRequest, query));
                if (!string.IsNullOrEmpty(form))
                    identity.AddClaim(new Claim(LtiClaimsViewModel.ClaimName.CourseRequest, form));
            }
        }

        
        private static async Task<string> LtiLogin(HttpContext context, string consumerSecret)
        {
            // lti login
            var ltiRequest = await context.Request.ParseLtiRequestAsync();
            
            //signature
            var signature = ltiRequest.GenerateSignature(consumerSecret);

            var oauthSignature = (string)context.Request.Form["oauth_signature"] ?? "0";

            if (signature != oauthSignature)
            {
                throw new IdentityHttpException(HttpStatusCode.Forbidden,"oauth_signature failed");
            }
            // end signature

            var session = (string)context.Request.Form["custom_canvas_course_id"] ?? "0";
            if (session == "0")
            {
                throw new IdentityHttpException(HttpStatusCode.NotFound,"Course session failed");
            }

            var claimForm = ClamesFromLtiFormHelper.CreateLtiClaimsFromForm(context.Request.Form, session);
            await ClaimsHelper.MergeUserClaimsAndSignIn(context, claimForm);
            
            return session;
        }
    }
}
