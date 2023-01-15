using Dapper.FluentMap;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using System;
using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Spolis.Attributes;
using Spolis.Filters;

namespace Spolis
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
          //  loggerFactory.AddSerilog();
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
                IdentityModelEventSource.ShowPII = true;

            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();
            app.UseRouting();

            //app.UseAuthentication();
            // app.UseAuthorization();
            //app.UseMvcWithDefaultRoute(); delete


            app.UseRequestLocalization();
            app.UseSession();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}");
                endpoints.MapRazorPages();
            });
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews().AddRazorRuntimeCompilation();
            services.AddRazorPages(); //delete?
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential 
                // cookies is needed for a given request.
                options.CheckConsentNeeded = context => false;
                // requires using Microsoft.AspNetCore.Http;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            // Dapper mapošanas inicializācija
            FluentMapper.Initialize(config =>
            {
                
                var addMethod = config.GetType().GetMethod(nameof(config.AddMap));
                foreach (var f in EntityModelHelper.CreateAllEntityMaps())
                {
                    addMethod.MakeGenericMethod(f.GetType().GetGenericArguments()[0]).Invoke(config, new[] { f });
                }

            });


            services.Configure<RequestLocalizationOptions>(options =>
            {
                options.DefaultRequestCulture = new RequestCulture(new CultureInfo(SpolisParameters.CultureInfoCode)
                {
                    DateTimeFormat = new DateTimeFormatInfo
                    {
                        ShortTimePattern = SpolisParameters.ShortTimePattern,
                        LongTimePattern = SpolisParameters.LongTimePattern,
                        ShortDatePattern = SpolisParameters.ShortDatePattern
                    },
                    NumberFormat = new NumberFormatInfo
                    {
                        NumberDecimalSeparator = ".",
                        NumberGroupSeparator = ".",
                        CurrencyDecimalSeparator = ".",
                        CurrencyGroupSeparator = ".",
                        PercentDecimalSeparator = ".",
                        PercentGroupSeparator = "."
                    }
                });
                options.RequestCultureProviders.Clear();
            });

            services.AddKendo();

            services.AddMvc(options =>
            {
                AuthorizationPolicy policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                options.Filters.Add(new AuthorizeFilter(policy));
               // options.Filters.Add(typeof(AuditFilter));
              //  options.Filters.Add(typeof(SessionFilter));
                options.Filters.Add(typeof(IndexRegistyFilter));
                options.Filters.Add(typeof(ModelRegistyFilter));
                options.Filters.Add(typeof(NavigationFilter));

                options.ModelBindingMessageProvider.SetValueIsInvalidAccessor(x => string.Format(SpolisResources.BndValueIsInvalid, x));
                options.ModelBindingMessageProvider.SetValueMustBeANumberAccessor(x => string.Format(SpolisResources.BndValueMustBeANumber, x));
                options.ModelBindingMessageProvider.SetMissingBindRequiredValueAccessor(x => string.Format(SpolisResources.BndMissingBindRequiredValue, x));
                options.ModelBindingMessageProvider.SetAttemptedValueIsInvalidAccessor((x, y) => string.Format(SpolisResources.BndAttemptedValueIsInvalid, x, y));
                options.ModelBindingMessageProvider.SetMissingKeyOrValueAccessor(() => SpolisResources.BndMissingKeyOrValue);
                options.ModelBindingMessageProvider.SetUnknownValueIsInvalidAccessor(x => string.Format(SpolisResources.BndUnknownValueIsInvalid, x));
                options.ModelBindingMessageProvider.SetValueMustNotBeNullAccessor(x => string.Format(SpolisResources.BndValueMustNotBeNull, x));
                options.ModelBindingMessageProvider.SetMissingRequestBodyRequiredValueAccessor(() => SpolisResources.BndMissingRequestBodyRequiredValue);
                options.ModelBindingMessageProvider.SetNonPropertyAttemptedValueIsInvalidAccessor(x => string.Format(SpolisResources.BndNonPropertyAttemptedValueIsInvalid, x));
                options.ModelBindingMessageProvider.SetNonPropertyUnknownValueIsInvalidAccessor(() => SpolisResources.BndNonPropertyUnknownValueIsInvalid);
                options.ModelBindingMessageProvider.SetNonPropertyValueMustBeANumberAccessor(() => SpolisResources.BndNonPropertyValueMustBeANumber);

            })

            .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
            .AddDataAnnotationsLocalization()
            .SetCompatibilityVersion(CompatibilityVersion.Version_3_0)
            .AddControllersAsServices()
            .AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ContractResolver = new DefaultContractResolver();
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            });

           // SystemParamStartupProcedure systemParamProcedure = new SystemParamStartupProcedure(Configuration.GetConnectionString(SpolisParameters.ConnectionStringName));
           // SystemParamViewModel sysParamView = systemParamProcedure.SystemParamGet(SpolisParameters.PrmSessionTimeout);

            // Autentifikācijas konfigurācija
            services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.Name = SpolisParameters.SpolisIdentityCookie;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.LoginPath = "/Home/Index";
                options.AccessDeniedPath = "/Account/AccessDenied";
                options.SlidingExpiration = true;
              //  options.ExpireTimeSpan = TimeSpan.FromMinutes(Convert.ToDouble(sysParamView.PrmValue));
            });

            // Sesijas konfigurācija             
            services.AddSession(options =>
            {
                options.Cookie.Name = SpolisParameters.SpolisSessionCookie;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                //options.IdleTimeout = TimeSpan.FromMinutes(Convert.ToDouble(sysParamView.PrmValue));
            });

            // Tiesību konfigurācija
            //PolicyConfiguration.SetAuthorizationPolicies(services);
        }
    }
}