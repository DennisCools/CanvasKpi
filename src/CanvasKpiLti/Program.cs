using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using CanvasIdentity.Exceptions;
using CanvasIdentity.Extensions;
using CanvasKpiLti.Services;
using CompetenceProfilingDomain.Contracts.Infrastructure;
using CompetenceProfilingDomain.Domain;
using CompetenceProfilingDomain.DomainCp;
using CompetenceProfilingInfrastructure.CanvasDao;
using CompetenceProfilingInfrastructure.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CanvasKpiLti;

public class Program
{
    static IConfiguration _configuration = new ConfigurationBuilder().Build();

    public static void Main(string[] args)
    {
       
        
        var builder = WebApplication.CreateBuilder(args);
        WebApplicationBuilder(builder);
        var app = builder.Build();
        app.Logger.LogInformation("DC0=>Info logger Start application ");
        Debug.WriteLine("DC0=>Debug logger Start application");
        
        _configuration = app.Configuration;
        
        app.UseCanvasUseExceptionHandler();
        if (!app.Environment.IsDevelopment())
        {
            
            string secLengtApp = app.Configuration["CanvasLti:ConsumerSecret"] ?? throw new InvalidOperationException("CanvasLti:ConsumerSecret not found.");
            if(secLengtApp.Length < 12)  throw new Exception("ConsumerSecret to short is required x> 12.");
            app.Logger.LogInformation("DC0=>ConsumerSecret to short check passed");
            //app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        //app.UseHttpsRedirection();
        
        app.UseForwardedHeaders();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthentication();
        
        //canvas course SignInAsync with CookieAuthenticationDefaults.AuthenticationScheme
        app.UseCanvasLtiCourseIdentity(option =>
            {
                option.ConsumerSecret = app.Configuration["CanvasLti:ConsumerSecret"] ?? throw new InvalidOperationException("CanvasLti:ConsumerSecret not found.");
                option.LtiCourseLoginPath = app.Configuration["CanvasLti:LtiCourseLoginPath"] ?? throw new InvalidOperationException("CanvasLti:LtiCourseLoginPath not found.");
                option.RedirectAfterLogin = app.Configuration["CanvasLti:RedirectAfterLogin"] ?? throw new InvalidOperationException("CanvasLti:RedirectAfterLogin not found.");
            }
        );
        app.UseAuthorization();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        app.Run();

    }

    private static void WebApplicationBuilder(WebApplicationBuilder builder)
    {
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        { //https://seankilleen.com/2020/06/solved-net-core-azure-ad-in-docker-container-incorrectly-uses-an-non-https-redirect-uri/
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                                       ForwardedHeaders.XForwardedProto;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });
        
        builder.Services.AddDbContext<IRepository, DatabaseContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("CanvasKpiLtiContext") ??
                                 throw new InvalidOperationException(
                                     "Connection string 'CanvasKpiLtiContext' not found.")));


        builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = "token";
                options.DefaultChallengeScheme = "CanvasOAuth";
            })
            .AddCookie(options =>
            {
                options.Cookie.Name = "PocCanvasLtiKpi";
                options.Cookie.SameSite = SameSiteMode.None;
                options.Events.OnRedirectToLogin = _ =>
                    throw new IdentityHttpException(HttpStatusCode.Forbidden, "Error Cookie login");
                options.Events.OnRedirectToAccessDenied = _ =>
                    throw new IdentityHttpException(HttpStatusCode.Forbidden, "Error Cookie login");
            }).AddCookie("token", options =>
            {
                options.Cookie.Name = "PocTokenKpiA";
                options.Cookie.SameSite = SameSiteMode.None;
                options.Events.OnRedirectToLogin = _ =>
                    throw new IdentityHttpException(HttpStatusCode.Forbidden, "Error Token Cookie login");
                options.Events.OnRedirectToAccessDenied = _ =>
                    throw new IdentityHttpException(HttpStatusCode.Forbidden, "Error Token Cookie login");
            })
            .AddOAuth("CanvasOAuth", options =>
                {
                    options.ClientId = _configuration["CanvasOAuth:ClientId"] ?? "";
                    options.ClientSecret = _configuration["CanvasOAuth:ClientSecret"] ?? "";
                    options.CallbackPath = new PathString(_configuration["CanvasOAuth:CallbackPath"]);
                    options.AuthorizationEndpoint = _configuration["CanvasOAuth:AuthorizationEndpoint"] ?? "";
                    options.TokenEndpoint = _configuration["CanvasOAuth:TokenEndpoint"] ?? "";
                    options.SaveTokens = true;
                }
            );

        builder.Services.AddControllersWithViews(options =>
        {
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
            options.Filters.Add(new AuthorizeFilter(policy));
        });

        
        
        builder.Services.AddDistributedSqlServerCache(options =>
        {
            options.ConnectionString = builder.Configuration.GetConnectionString("CanvasKpiLtiContext")
                                       ?? throw new InvalidOperationException(
                                           "Connection string 'CanvasKpiLtiContext' not found.");
            options.SchemaName = "dbo";
            options.TableName = "WebCache";
        });
        builder.Services.AddHttpContextAccessor();
        
        
        builder.Services.AddTransient<TokenStore>();

        builder.Services.AddHttpClient<ICanvasGraphQlHttpClient, CanvasGraphQlHttpClient>(client =>
        {
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.BaseAddress = new Uri(_configuration["CanvasApi:baseUrl"] + "/api/graphql");
        });

        builder.Services.AddHttpClient<ICanvasRestHttpClient, CanvasRestHttpClient>(client =>
        {
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.BaseAddress = new Uri(_configuration["CanvasApi:baseUrl"]!);
        });

        // Infrastructure
        builder.Services.AddTransient<IAssignmentGroupsDao, AssignmentGroupsDao>();
        builder.Services.AddTransient<IAssignmentRubricCriteriaRatingDao, AssignmentRubricCriteriaRatingDao>();
        builder.Services.AddTransient<IAssignmentRubricDao, AssignmentRubricDao>();
        builder.Services.AddTransient<IStudentDao, StudentDao>();

        //Domain
        builder.Services.AddTransient<CardCollection>();
        builder.Services.AddTransient<UserCollection>();
        builder.Services.AddTransient<AssignmentGroups>();
        builder.Services.AddTransient<IOutcomeResultCollection,OutcomeResultCollection>();
        builder.Services.AddTransient<Matrix>();
    }
}