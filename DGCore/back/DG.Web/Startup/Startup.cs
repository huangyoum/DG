﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DG.EntityFramework;
using Microsoft.EntityFrameworkCore;
using DG.Application;
using Microsoft.Extensions.Logging;
using DG.Application.Member;
using ACC.Application;
using NLog.Extensions.Logging;
using NLog.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.IO;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using ACC.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using ACC.Convert;
using DG.Application.Users;

namespace DG.Web
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                     .SetBasePath(env.ContentRootPath)
                     .AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(),"Configs", "appsettings.json"), optional: true, reloadOnChange: true)  
                     .AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(),"Configs", $"appsettings.{env.EnvironmentName}.json"), optional: true, reloadOnChange: true)
                     .AddEnvironmentVariables();                                              
            Configuration = builder.Build();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            /***********HttpContext相关设置（没搞懂现在不需要任何注入也TM有了。。。）************/
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            #region MyRegion
            /* MemoryCache */
            services.AddMemoryCache();
            /* Session */
            services.AddSession();
            /* Cookie */
            //services.AddAuthentication(options =>
            //{
            //    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            //}).AddCookie();

            //services.AddMvc(options =>
            //{
            //    options.Filters.Add(typeof(HttpGlobalExceptionFilter));
            //}).AddControllersAsServices(); 
            #endregion

            #region DbConnection
            var DbType = Configuration.GetConnectionString("DbType");
            if (DbType == "Sqlite")
            {
                services.AddDbContext<DGDbContext>(options => options.UseSqlite(Configuration.GetConnectionString(DbType)));
            }
            else if (DbType == "MsSql")
            {
                services.AddDbContext<DGDbContext>(options => options.UseSqlServer(Configuration.GetConnectionString(DbType)));
            }
            else if (DbType == "PostgreSql")
            {
                //services.AddDbContext<DGDbContext>(options => options.UseMySql(Configuration.GetConnectionString(DbType)));
            }
            else if (DbType == "MySQL")
            {
                services.AddDbContext<DGDbContext>(options => options.UseMySql(Configuration.GetConnectionString(DbType)));
            }
            else
            {
                services.AddDbContext<DGDbContext>(options => options.UseMySql(Configuration.GetConnectionString(DbType)));
            } 
            #endregion


            /* 注册服务 */
            services.AddServices();

            #region jwt
            var audienceConfig = Configuration.GetSection("Audience");
            var symmetricKeyAsBase64 = audienceConfig["Secret"];
            var keyByteArray = Encoding.ASCII.GetBytes(symmetricKeyAsBase64);
            var signingKey = new SymmetricSecurityKey(keyByteArray);

            var tokenValidationParameters = new TokenValidationParameters
            {

                // The signing key must match!
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = signingKey,

                // Validate the JWT Issuer (iss) claim
                ValidateIssuer = true,
                ValidIssuer = audienceConfig["Issuer"],

                // Validate the JWT Audience (aud) claim
                ValidateAudience = true,
                ValidAudience = audienceConfig["Audience"],

                // Validate the token expiry
                ValidateLifetime = true,

                ClockSkew = TimeSpan.Zero
            };
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(o =>
            {
                //不使用https
                //o.RequireHttpsMetadata = false;
                o.TokenValidationParameters = tokenValidationParameters;
            });
            #endregion
            
            services.AddMvc()
                .AddJsonOptions(options =>
                {
                    //忽略循环引用
                    //options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                    //不使用驼峰样式的key
                    options.SerializerSettings.ContractResolver = new DefaultContractResolver();

                    //日期类型默认格式化处理  
                    options.SerializerSettings.DateFormatHandling = Newtonsoft.Json.DateFormatHandling.MicrosoftDateFormat;
                    options.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss";
                    
                    //空值处理  
                    //options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                    
                    //默认转换  失败
                    //options.SerializerSettings.Converters.Add(new LongConverter());
                    //options.SerializerSettings.Converters.Add(new BoolConverter());
                });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            /* NLog */
            env.ConfigureNLog(Path.Combine(Directory.GetCurrentDirectory(),"Configs", "nlog.config"));
            loggerFactory.AddNLog();
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            #region JWT
            var audienceConfig = Configuration.GetSection("Audience");
            var symmetricKeyAsBase64 = audienceConfig["Secret"];
            var keyByteArray = Encoding.ASCII.GetBytes(symmetricKeyAsBase64);
            var signingKey = new SymmetricSecurityKey(keyByteArray);
            var SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
            app.UseAuthentication(new TokenProviderOptions
            {
                Audience = audienceConfig["Audience"],
                Issuer = audienceConfig["Issuer"],
                SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)
            });
            #endregion

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseSession();
            
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");

                #region api路由
                /****api路由****
                        webapi解耦（随时可以独立出去） 使用DG.Controllers.Api(引用就可以)有多种方法：采用中间件方式，Application Model等这里介绍用简单路由配置的方式：
                         1、使用路由：添加路由规则即可
                             首先
                             routes.MapRoute(
                                name: "api",
                                template: "api/{controller}/{action}/{id?}"
                                );
                             然后在控制器上添加标签关键是"api"前缀名称要相同
                             [Produces("application/json")]
                             [Route("api/[controller]/[action]")]
                             public class ApiBaseController : Controller
                             {
                             }
                         2、使用域
                             首先添域路由规则
                             routes.MapAreaRoute("api_route", "api","api/{controller}/{action}/{id?}");
                             然后在控制器添加标签关键是“api”域名称要相同
                             [Produces("application/json")]
                             [Area("api")]
                             public class ApiBaseController : Controller
                             {
                             }
                        ****/
                #endregion

                routes.MapAreaRoute(
                    "area_api",
                    "api",
                    "api/{controller}/{action}/{id?}");
            });
        }
    }
}
