using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using CanvasIdentity.Extensions;
using CanvasKpi.Models;
using CanvasKpiLti.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

namespace CanvasKpiLti.Controllers
{
    public class HomeController : Controller
    {
     
        private readonly IDistributedCache _cache;
        private readonly TokenStore _tokenStore;

        public HomeController(IDistributedCache cache, TokenStore tokenStore)
        {
            _cache = cache;
            _tokenStore = tokenStore;
        }
        
        public  IActionResult DoOauth(string session)
        { // called after LTI.
            var redirect = $@"/AssignmentRubric/index/?session={session}";
             var token =  HttpContext.GetTokenAsync("token","access_token").Result;
             if (token == null)
             {
                 return Challenge(new AuthenticationProperties()
                 {
                     RedirectUri = redirect ,AllowRefresh = true
                 }, "CanvasOAuth");
             }

            return LocalRedirect(redirect);
        }
        
        public IActionResult HomeIndex(string session)
        {
            if (User.CanvasClaims().IsCanvasInstructor() == false) return NotFound();
            var token = _tokenStore.Token;
            ViewData["token"] = token;
            ViewData["session"] = session;

            string cachedTimeUtc;
            
            var encodedCachedTimeUtc = _cache.Get("cachedTimeUTC");
            
            if (encodedCachedTimeUtc != null)
            {
                cachedTimeUtc = Encoding.UTF8.GetString(encodedCachedTimeUtc);
            }
            else
            {
                var currentTimeUtc = DateTime.UtcNow.ToString(CultureInfo.CurrentCulture);
                byte[] encodedCurrentTimeUtc = Encoding.UTF8.GetBytes(currentTimeUtc);
                var options = new DistributedCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromSeconds(20));
                 _cache.Set("cachedTimeUTC", encodedCurrentTimeUtc, options);
                 cachedTimeUtc = "New time Cached";
            }
            
            cachedTimeUtc = DateTime.UtcNow.ToString(CultureInfo.CurrentCulture);
            
            ViewData["cachedTimeUTC"] = cachedTimeUtc;
            return View();
        }
        
        public IActionResult Chris()
        {
            return View();
        }
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}