using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using LtiLibrary.NetCore.Lti.v1;
using LtiLibrary.NetCore.OAuth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace CanvasIdentity.LtiLibrary.AspNetCore.Extensions
{
    public static class HttpRequestExtensions
  {
    
    
    public static bool IsAuthenticatedWithLti(this HttpRequest request)
    {
      if (!request.HasFormContentType)
        return request.ParseAuthenticationHeader().HasKeys();
      StringValues stringValues = request.Form["lti_message_type"];
      string messageType = stringValues.Count > 0 ? stringValues[0] : string.Empty;
      return HttpRequestExtensions.PostMessageIsAuthenticated(request, messageType);
    }

    public static async Task<bool> IsAuthenticatedWithLtiAsync(this HttpRequest request)
    {
      if (!request.HasFormContentType)
        return request.ParseAuthenticationHeader().HasKeys();
      StringValues stringValues = (await request.ReadFormAsync())["lti_message_type"];
      return HttpRequestExtensions.PostMessageIsAuthenticated(request, stringValues.Count > 0 ? stringValues[0] : string.Empty);
    }

    private static bool PostMessageIsAuthenticated(HttpRequest request, string messageType)
    {
      if (!request.Method.Equals("POST"))
        return false;
      return messageType.Equals("basic-lti-launch-request", StringComparison.OrdinalIgnoreCase) || messageType.Equals("ContentItemSelectionRequest", StringComparison.OrdinalIgnoreCase) || messageType.Equals("ContentItemSelection", StringComparison.OrdinalIgnoreCase);
    }

    private static NameValueCollection ParseAuthenticationHeader(this HttpRequest request)
    {
      NameValueCollection authenticationHeader = new NameValueCollection();
      StringValues header = request.Headers["Authorization"];
      if (header.Count == 0)
        return authenticationHeader;
      string str1 = header[0];
      int length = str1.IndexOf(' ');
      if (!str1.Substring(0, length).Equals("OAuth"))
        return authenticationHeader;
      string str2 = str1.Substring(length + 1);
      char[] chArray1 = new char[1]{ ',' };
      foreach (string str3 in str2.Split(chArray1))
      {
        char[] chArray2 = new char[1]{ '=' };
        string[] strArray = str3.Split(chArray2);
        string key = strArray[0].Trim();
        if (((IEnumerable<string>) OAuthConstants.OAuthParameters).Any<string>((Func<string, bool>) (p => p.Equals(key))))
        {
          string str4 = WebUtility.UrlDecode(strArray[1].Trim(new char[1]
          {
            '"'
          }));
          authenticationHeader.Set(key, str4);
        }
      }
      return authenticationHeader;
    }

    public static async Task<LtiRequest> ParseLtiRequestAsync(this HttpRequest request)
    {
      //request.
      LtiRequest ltiRequest1 = new LtiRequest((string) null);
      ltiRequest1.Url = request.GetUri();
      Debug.WriteLine("DCO=>"+ltiRequest1.Url);
      ltiRequest1.HttpMethod = request.Method;
      LtiRequest ltiRequest = ltiRequest1;
      if (request.HasFormContentType)
      {
        foreach (KeyValuePair<string, StringValues> keyValuePair in (IEnumerable<KeyValuePair<string, StringValues>>) await request.ReadFormAsync().ConfigureAwait(false))
        {
          foreach (string str in keyValuePair.Value)
            ltiRequest.AddParameter(keyValuePair.Key, str);
        }
        return ltiRequest;
      }
      NameValueCollection authenticationHeader = request.ParseAuthenticationHeader();
      foreach (string allKey in authenticationHeader.AllKeys)
      {
        string[] values = authenticationHeader.GetValues(allKey);
        if (values != null)
        {
          foreach (string str in values)
            ltiRequest.AddParameter(allKey, str);
        }
      }
      if (request.Headers["BodyHash"].Any<string>())
        ltiRequest.BodyHashReceived = request.Headers["BodyHash"].First<string>();
      return ltiRequest;
    }

    public static Uri GetUri(this HttpRequest request)
    {
      string[] strArray = request.Host.ToUriComponent().Split(new char[1]
      {
        ':'
      });
      UriBuilder uriBuilder = new UriBuilder()
      {
        Scheme = request.Scheme,
        Host = strArray[0],
        Path = (string) (request.PathBase + request.Path),
        Query = request.QueryString.ToUriComponent()
      };
      if (strArray.Length == 2)
        uriBuilder.Port = Convert.ToInt32(strArray[1]);
      return uriBuilder.Uri;
    }
  }
}