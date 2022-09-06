using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.OData.Authorization.Extensions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ODataAuthorizationDemo.Models;
using Microsoft.AspNetCore.OData.Authorization;
using Microsoft.AspNetCore.OData;

namespace ODataAuthorizationDemo
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase("ODataAuthDemo"));

            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll",
                    builder =>
                    {
                        builder
                            .AllowAnyOrigin()
                            .AllowAnyMethod()
                            .AllowAnyHeader();
                    });
            });

            services
                .AddControllers()
                .AddOData((opt) =>
                {
                    opt.RouteOptions.EnableActionNameCaseInsensitive = true;
                    opt.RouteOptions.EnableControllerNameCaseInsensitive = true;
                    opt.RouteOptions.EnablePropertyNameCaseInsensitive = true;

                    opt
                        .AddRouteComponents("odata", AppEdmModel.GetModel())
                        .EnableQueryFeatures().Select().Expand().OrderBy().Filter().Count();
                });

            services.AddODataAuthorization(options =>
            {
                options.ScopesFinder = context =>
                {
                    var scopes = context.User.FindAll("Scope").Select(claim => claim.Value);

                    return Task.FromResult(scopes);
                };

                options.ConfigureAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors("AllowAll");

            app.UseRouting();

            app.UseAuthentication();

            app.UseODataAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
