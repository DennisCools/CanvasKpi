using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using CompetenceProfilingInfrastructure.CanvasDao.HttpClientExtensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace CanvasKpiLti.Services;

public class TokenStore
{
    private readonly IConfiguration _configuration;
    private readonly HttpContext _httpContext;
    private readonly IHttpClientFactory _httpClientFactory;

    private string? _token;
    public string? Token {
        get
        {
            Debug.WriteLine("Get token");
            RefreshTokenIfExpired();
            return _token;
        }
    }

    private string RefreshToken { get; set; } = "No Refresh";
    private DateTime ExpiresAt { get; set; }


    public TokenStore(IHttpContextAccessor httpContextAccessor, IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _httpContext = httpContextAccessor.HttpContext ?? throw new Exception("httpContext is null");
        _httpClientFactory = httpClientFactory;
        
        Debug.WriteLine("GetToken from httpContextAccessor");
        _token = _httpContext.GetTokenAsync("token", "access_token").Result;
        if (_token == null) return;
        RefreshToken = _httpContext.GetTokenAsync("token", "refresh_token").Result ?? "No Refresh";
        ExpiresAt = DateTime.Parse(_httpContext.GetTokenAsync("token", "expires_at").Result ?? "No expires_at");
    }

    private void RefreshTokenIfExpired()
    {
#if DEBUG
        var expiresAt = ExpiresAt - TimeSpan.FromMinutes(55); //=> RefreshToken after 5 min
#else
                var expiresAt = ExpiresAt - TimeSpan.FromMinutes(10);
#endif
        if( expiresAt < DateTime.Now)
            GetTokenByRefreshToken();
    }
   
    private void GetTokenByRefreshToken()
    {
        var authenticateResult = _httpContext.AuthenticateAsync("token").Result ??
                                 throw new Exception("No AuthenticateResult");

        DoRefToken(_configuration["CanvasOAuth:ClientId"] ?? "",
            _configuration["CanvasOAuth:ClientSecret"] ?? "", RefreshToken,
            _configuration["CanvasOAuth:TokenEndpoint"] ?? "");

        if (authenticateResult.Properties == null) return;
        if (_token != null) authenticateResult.Properties.UpdateTokenValue("access_token", _token);
        authenticateResult.Properties.UpdateTokenValue("expires_at", ExpiresAt.ToString(CultureInfo.CurrentCulture));
        var principal = authenticateResult.Principal;
        if (principal == null) throw new Exception("No authenticateResult.Principal");
        _httpContext.SignInAsync("token", principal, authenticateResult.Properties);
    }
    
    private void DoRefToken(string oauthClientId, string oauthClientSecret, string refreshToken, string oauthTokenUrl)
    {
        var pairs = new List<KeyValuePair<string, string>>
        {
            new ("grant_type", "refresh_token"),
            new ("client_id", oauthClientId),
            new ("client_secret", oauthClientSecret),
            new ("refresh_token", refreshToken)
        };
        var queryString = new FormUrlEncodedContent(pairs);
        using var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("User-Agent", "CanvasKpi");
        var response =  client.PostAsync(oauthTokenUrl, queryString).Result;

        if (response.IsSuccessStatusCode)
        {
            var result = JsonConvert.DeserializeObject<dynamic>(
                response.Content.ReadAsStringAsync().Result);

            _token = result?.access_token ?? "";
            string expiresInSec = result?.expires_in ?? "0";
            var secToExpires = int.Parse(expiresInSec);
            ExpiresAt = DateTime.Now + TimeSpan.FromSeconds(secToExpires);
            
            Debug.WriteLine("New token");
        }
        else
        {
            throw new CanvasHttpClientGetAllPagesExtension.CanvasHttpException(response.StatusCode,  response.Content.ReadAsStringAsync().Result);
        }
    }
}