using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Cors;//.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using DLAdjApi.Models;
using DLAdjApi.Services;
using log4net.Extensions.AspNetCore;
using System.Xml;
using System.IO;
using System.Reflection;
using log4net.Repository;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using System.Runtime.Loader;
using Swashbuckle.AspNetCore.Swagger;
using Microsoft.Extensions.PlatformAbstractions;
using log4net;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.StaticFiles;
using System.IO.MemoryMappedFiles;
using System.Threading;
using System.Collections.Concurrent;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Microsoft.Extensions.Hosting;

namespace DLAdjApi
{    
    public static class LifeguardThread 
    {
        public static bool close = false;
    }

    public static class ApInfo
    {
        public static string Fab = ""; 
        public static string Version = "4.2.4";  
        public static string Ip = "";
    }

    public static class ApAnalysisSetting
    {
        public static string Url {get; set;}
        public static string PreStr {get; set;}
    }    

    public static class Global_ModelConstraint
    {
        public static ConcurrentDictionary<string, Global_Data> Constraint = new ConcurrentDictionary<string, Global_Data>();   //Shop, Constraint
    }

    public class Global_Data
    {
        public DateTime UpdateTime;
        public JsonResult Data;
    }

    public class DirectorySettings
    {
        public DirectorySettings()
        {
            //local disk default path
            WebRoot = @"D:/DataFile/AIAdj";
            WebRootVDisk = @"/WebFolder";
            WebRoot_Sample = @"D:/WebAp/AIAdj/api/sample";
            WebRootVDisk_Sample = @"/Sample";            
            IsModelServer = false;
        }
        public bool IsModelServer {get; set;}        
        public string Root {get; set;}        
        public string RootVDisk {get; set;}
        public string Un {get; set;}
        public string Pwd {get; set;} 
        public string WebRoot {get; set;}        
        public string WebRootVDisk {get; set;}
        public string WebRoot_Sample {get; set;}        
        public string WebRootVDisk_Sample {get; set;}        
    }
    
    public class ShopSettings
    {
        public string[] Shop {get; set;}
        public bool ModelServerFilter {get; set;}
        public string MesApiRootUrl {get; set;}
        //public bool ResizeImage {get; set;}
    }

    public class UmdSetting
    {
        public string Url {get; set;}
        public string AlarmId {get; set;}
    }

    public class Startup
    {
        public static ILoggerRepository repo {get; set;}
        public static ILog log {get;set;}

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;                        

            //Log4Net
            XmlDocument log4netConfig = new XmlDocument();
            log4netConfig.Load(File.OpenRead(@".\log4net.config"));
            repo = log4net.LogManager.CreateRepository(Assembly.GetEntryAssembly(), typeof(log4net.Repository.Hierarchy.Hierarchy));
            log4net.Config.XmlConfigurator.Configure(repo, log4netConfig["log4net"]);                        
            log = log4net.LogManager.GetLogger(typeof(Program));            
        }
        
        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //services.AddHttpClient();     //.net core 2.1 以上
            services.AddMvc();
            services.AddIdentity<IdentityUser, IdentityRole>(options =>
            {
                
            })
            .AddEntityFrameworkStores<DLAdjContext>()
            .AddDefaultTokenProviders();
            // ===== Add Jwt Authentication ========
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear(); // => remove default claims
            services
                .AddAuthentication(auth =>
                {   auth.DefaultAuthenticateScheme = "inx-jwt";
                    auth.DefaultChallengeScheme = "inx-jwt";
                    // options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    // options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                    // options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

                })
                .AddJwtBearer("inx-jwt", options => { 
                    // options.SecurityTokenValidators.Clear();
                    // options.SecurityTokenValidators.Add(new TokenValidator("inx-jwt"));
                    
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new RsaSecurityKey(RsaJwk.RsaJwkExtensions.GetRSAParameters(Program.Jwk)), // Key
                        ValidateLifetime = false, // 驗證時間
                        RequireExpirationTime = false, // 是不是有過期時間
                        ValidateAudience = false,
                        ValidateIssuer = false,
                        ClockSkew = TimeSpan.Zero // 時間偏移
                    };
                    options.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = context =>
                        {
                            // Add the access_token as a claim, as we may actually need it
                            var accessToken = context.SecurityToken as JwtSecurityToken;

                            return Task.CompletedTask;
                        }
                    };
                });        

            services.AddCors(options =>
            {
                options.AddPolicy("AllowAnyOriginPolicy", builder => builder
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    //.AllowCredentials()   //3.0不能與origin同用戶
                );
            });
            services.AddMvc(config => {
                config.Filters.Add(new ActionFilter());     //在 Controller->Action 之 前/後 執行
            });
            services.Configure<MvcOptions>(options =>
            {
                options.EnableEndpointRouting = false;      //app.UseMvc 會出現 Endpoint 異常
            });  

            //替代下面 ?
            services.AddControllers()
                    .AddNewtonsoftJson(options =>
                    {
                        options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;    //避免因 Table 之間的關聯性, 導致無限關聯
                        options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                    });      
            //.net 3.0不支援
            //services.AddMvc().AddJsonOptions(options => {
            //    options.SerializerSettings.ReferenceLoopHandling =
            //        Newtonsoft.Json.ReferenceLoopHandling.Ignore;
            //});
                    

            /*
            .net 3.0不支援
            services.Configure<MvcOptions>(options =>
            {
                options.Filters.Add(new CorsAuthorizationFilterFactory("AllowAnyOriginPolicy"));
            });
            */

            services.Configure<DirectorySettings>(Configuration.GetSection("DirectorySettings"));
            services.Configure<ShopSettings>(Configuration.GetSection("ShopSettings"));
            services.Configure<UmdSetting>(Configuration.GetSection("UmdSetting"));
        
            //傳送檔案大小限制
            services.Configure<FormOptions>(x =>
            {
                x.ValueLengthLimit = int.MaxValue;
                x.MultipartBodyLengthLimit = long.MaxValue; 
            });

            using(var context = new DLAdjContext())
            {
                //context.Database.EnsureDeleted(); 
                //Creates the database if not exists                
                //context.Database.EnsureCreated();                 
                /*
                改由指令建Table
                Drop DB : dotnet ef database drop --context DLAdjContext               
                Add/Modify : 
                            dotnet ef migrations add xxxInitial --context DLAdjContext
                            dotnet ef database update --context DLAdjContext
                */
            }
            
            services.AddTransient<IModelService, MySqlModelService>();                       
            services.AddDbContext<DLAdjContext>();
            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>()
                    .AddScoped<IUrlHelper>(x => x
                                            .GetRequiredService<IUrlHelperFactory>()
                                            .GetUrlHelper(x.GetRequiredService<IActionContextAccessor>().ActionContext));

            //Register the Swagger generator, defining 1 or more Swagger documents            
            services.AddSwaggerGen(c =>
            {                
                //c.SwaggerDoc("Api", new Info { Title = "AI.Adj Web Service", Version = "Ver 3.1.6" });
                c.SwaggerDoc("Api", new OpenApiInfo { Title = "AI.Adj Web Service", Version = "Ver " + ApInfo.Version });
                var filePath = Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "Api.xml");   //會產出一份 Api.xml 於 debug/release/publish 目錄裡
                c.IncludeXmlComments(filePath);

                //加入 Header 選項 Authorization
                //c.OperationFilter<HeaderFilter>();  //.Net Core 3.0 以上改寫法
                c.AddSecurityDefinition("Bearer", 
                    new OpenApiSecurityScheme
                    {
                        Name = "Authorization",
                        Type = SecuritySchemeType.ApiKey,
                        Scheme = "Bearer",
                        BearerFormat = "JWT",
                        In = ParameterLocation.Header,
                        Description = "JWT Authorization"
                    });
                c.AddSecurityRequirement(
                    new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "Bearer"
                                }
                            },
                            new string[] {}
                        }
                    });
            });        
                      
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();      //取得client資訊                

            //取得 Ap Analysis API 資訊
            if (string.IsNullOrEmpty(Configuration.GetSection("ApAnalysisSetting:Url").Value) == false)
            {
                ApAnalysisSetting.Url = Configuration.GetSection("ApAnalysisSetting:Url").Value;
            }
            else
            {
                ApAnalysisSetting.Url = "";
            }            
            if (string.IsNullOrEmpty(Configuration.GetSection("ApAnalysisSetting:PreStr").Value) == false)
            {
                ApAnalysisSetting.PreStr = Configuration.GetSection("ApAnalysisSetting:PreStr").Value;
            }        
            else
            {
                ApAnalysisSetting.PreStr = "/";
            }           

            //Fab資訊
            if (string.IsNullOrEmpty(Configuration.GetSection("Fab").Value) == false)
            {
                ApInfo.Fab = Configuration.GetSection("Fab").Value;      
            }        

            //AP 所在的 Server IP
            ApInfo.Ip = Tools.GetLocalIp();   

            //For initial Global_Parameter
            Global_ModelConstraint.Constraint.Count();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.        
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)   //public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            bool TestEnvironment = false;
            if (Configuration.GetSection("TestEnvironment").Value != null)
            {
                TestEnvironment = Convert.ToBoolean(Configuration.GetSection("TestEnvironment").Value);
            }

            if (Convert.ToBoolean(Configuration.GetSection("TraceLog").Value) == true)
            {
                loggerFactory.AddLog4Net();
            }
                        
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                // app.UseHsts();          
            }
                     
            //Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();
            //app.UseSwaggerUI();

            
            //Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), 
            //specifying the Swagger JSON endpoint.
            string title = "AI.Adj Web Service";
            bool isModelServer = false;
            bool clearNetConnection = true;
            if (string.IsNullOrEmpty(Configuration.GetSection("DirectorySettings:IsModelServer").Value) == false && Configuration.GetSection("DirectorySettings:IsModelServer").Value.ToLower() == "true")           
            {
                isModelServer = true;                
                clearNetConnection = false;     //Model Server 只連本機磁碟, 所以不會有網路磁碟機
                title = "at Model Server";
            }

            //網路磁碟 
            Tools.NetConnection(Configuration.GetSection("DirectorySettings:Root").Value, Configuration.GetSection("DirectorySettings:Un").Value, Configuration.GetSection("DirectorySettings:Pwd").Value, clearNetConnection);   //Tools.NetConnection("//10.75.1.208/Uploadstest/", Configuration.GetSection("DirectorySettings:Un").Value, Configuration.GetSection("DirectorySettings:Pwd").Value, true);   

            app.UseSwaggerUI(c =>
            {//Swagger/Api/Swagger.json
                c.SwaggerEndpoint("./Swagger/Api/Swagger.json", title);
                c.RoutePrefix = string.Empty;   //swagger url 改成根節點開始
                //c.RoutePrefix = "api";
            });
    
            app.UseAuthentication();
            app.UseDefaultFiles();           
            app.UseRouting();            
            app.UseCors("AllowAnyOriginPolicy");
            app.UseMvc();             

            //app.UseStaticFiles();            

            //Model Server
            //if (isModelServer == true)
            //{
                //支援下載 zip.001
                var provider = new FileExtensionContentTypeProvider();
                for(int i=1;i<=20;i++)
                {
                    provider.Mappings["." + i.ToString().PadLeft(3,'0')] = "application/octet-stream";      //for zip 分割檔 .001, .002
                }         
                try
            {
                string rFolder = $"{Configuration.GetSection("DirectorySettings:Root").Value}output/";     
                app.UseStaticFiles(new StaticFileOptions
                    {
                    //FileProvider = new PhysicalFileProvider(Configuration.GetSection("DirectorySettings:Root").Value),
                    FileProvider = new PhysicalFileProvider(rFolder),
                    RequestPath = new PathString(Configuration.GetSection("DirectorySettings:RootVDisk").Value),                            
                        ContentTypeProvider = provider
                    });                
                }
                catch(Exception ex)
                {
                    log.Error(ex.Message);
                }

                //Model Server 的 Upload 目錄
                try
                {
                //if (isModelServer == true)
                //{output\\
                string _folder= $"{Configuration.GetSection("DirectorySettings:Root").Value}Output/";
                app.UseFileServer(new FileServerOptions() {
                    //FileProvider = new PhysicalFileProvider(Configuration.GetSection("DirectorySettings:Root").Value),
                    FileProvider = new PhysicalFileProvider(_folder),
                    RequestPath = new PathString(Configuration.GetSection("DirectorySettings:RootVDisk").Value),
                            EnableDirectoryBrowsing = true 
                        }); 
                        
                    //}
                }
                catch(Exception ex)
                {
                    log.Error(ex.Message);
                }                
            //}
            //else 
            //{
                //Web Server
                try
                {                  
                    //WebFolder  
                    DirectorySettings tmpDS = new DirectorySettings();
                    string tmpPath = ((string.IsNullOrEmpty(Configuration.GetSection("DirectorySettings:WebRoot").Value)) == true) ? tmpDS.WebRoot : Configuration.GetSection("DirectorySettings:WebRoot").Value;
                    if (Directory.Exists(tmpPath) == false)
                    {
                        Directory.CreateDirectory(tmpPath);
                    }
                    app.UseFileServer(new FileServerOptions() {
                        FileProvider = new PhysicalFileProvider(((string.IsNullOrEmpty(Configuration.GetSection("DirectorySettings:WebRoot").Value)) == true) ? tmpDS.WebRoot : Configuration.GetSection("DirectorySettings:WebRoot").Value),
                        RequestPath = new PathString((string.IsNullOrEmpty(Configuration.GetSection("DirectorySettings:WebRootVDisk").Value) == true) ? tmpDS.WebRootVDisk : Configuration.GetSection("DirectorySettings:WebRootVDisk").Value),                    
                        EnableDirectoryBrowsing = true 
                    });       
                    tmpDS = null;

                    //Sample Folder
                    tmpDS = new DirectorySettings();
                    tmpPath = ((string.IsNullOrEmpty(Configuration.GetSection("DirectorySettings:WebRoot_Sample").Value)) == true) ? tmpDS.WebRoot_Sample : Configuration.GetSection("DirectorySettings:WebRoot_Sample").Value;
                    if (Directory.Exists(tmpPath) == false)
                    {
                        Directory.CreateDirectory(tmpPath);
                    }
                    app.UseFileServer(new FileServerOptions() {
                        FileProvider = new PhysicalFileProvider(((string.IsNullOrEmpty(Configuration.GetSection("DirectorySettings:WebRoot_Sample").Value)) == true) ? tmpDS.WebRoot_Sample : Configuration.GetSection("DirectorySettings:WebRoot_Sample").Value),
                        RequestPath = new PathString((string.IsNullOrEmpty(Configuration.GetSection("DirectorySettings:WebRootVDisk_Sample").Value) == true) ? tmpDS.WebRootVDisk_Sample : Configuration.GetSection("DirectorySettings:WebRootVDisk_Sample").Value),                    
                        EnableDirectoryBrowsing = true 
                    });       
                    tmpDS = null;
                }
                catch(Exception ex)
                {
                    log.Error(ex.Message);
                }
            //}

/*
            app.UseFileServer(new FileServerOptions() {
                FileProvider = new PhysicalFileProvider("//10.75.1.208/Uploadstest/"),
                RequestPath = new PathString("/Uploads_ae"),
                EnableDirectoryBrowsing = true 
            });   
*/

            //Lifeguard
            if (isModelServer == true && TestEnvironment == false)
            {
                Thread thread = new Thread(new ThreadStart(SHM));
                thread.Start();
            }                            
        }   
        
        public void SHM()        //public static void SHM()    
        {            
            //bool checkMemory = false;
            string shm = "AIAdjApi_Model";            
            log.Info("Lifeguard SHM : " + shm);
            Console.WriteLine("Lifeguard SHM : " + shm);

            Lifeguard lg = new Lifeguard(log);
            while (true)
            {               
                lg.SendHeartBit(shm);
                Thread.Sleep(10000);
                if (LifeguardThread.close == true)
                {
                    Console.WriteLine("Thread End.");
                    break;
                }
             
            } 
            lg = null;
        }    

        public void ReleaseMemory(int sizeMB=1500)
        {
            long sizeByte = GC.GetTotalMemory(false);
            log.Info("[GC] " + (sizeByte/1024/1024) + " MB");  
            //Console.WriteLine("[GC] " + (sizeByte/1024/1024) + " MB");   

            if ((sizeByte/1024/1024) >= sizeMB)
            {                                
                GC.Collect();               
                Thread.Sleep(2000);     
                log.Info("[GC] After : " + (sizeByte/1024/1024) + " MB");   
                //Console.WriteLine("[GC] After : " + (sizeByte/1024/1024) + " MB");   
            }

        }        
    }
    public static class GlobalVariables {
        public static Dictionary<string, DateTime> ConvergenceToUmdTime = new Dictionary<string, DateTime>();

        //超過一天的記錄要刪除
        public static void CheckTimeOut()
        {
            List<string> tmp = new List<string>();
            foreach(var k in ConvergenceToUmdTime.Keys)
            {
                if (GlobalVariables.ConvergenceToUmdTime[k].AddDays(1) <= DateTime.Now) 
                {
                    tmp.Add(k);                    
                }
            }
            for(int i=0; i<tmp.Count; i++)
            {
                GlobalVariables.ConvergenceToUmdTime.Remove(tmp[i]);
            }
        }      
    }

    public class Lifeguard
    {
        private ILog log {get;set;}
        public Lifeguard(ILog _log)
        {
            log = _log;
        }

        /// <summary>
        /// 與 Lifeguard 互通, 傳送HeartBit
        /// </summary>
        /// <param name="HeartBit"></param>
        public void SendHeartBit(string HeartBit)
        {
            try
            {
                WriteSHM(HeartBit, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));                
            }
            catch (Exception ex)
            {
                if (log != null)
                {
                    Console.WriteLine("[Lifeguard] " + ex.Message);   
                    log.Error("[Lifeguard] " + ex.Message);   
                }
                else
                {
                    Console.WriteLine("[Lifeguard] " + ex.Message);   
                }                
            }
        }

        /// <summary>
        /// 寫入 SHM
        /// </summary>
        /// <param name="SHM"></param>
        /// <param name="Data"></param>
        /// <returns></returns>
        public void WriteSHM(string SHM, string Data)
        {
            MemoryMappedFile mmf = MemoryMappedFile.OpenExisting(SHM);
            MemoryMappedViewStream mmvs = mmf.CreateViewStream();
            BinaryWriter bw = new BinaryWriter(mmvs);
            bw.Write(Data);
            bw.Close();
            bw.Dispose();
            bw = null;
            mmvs.Close();
            mmvs.Dispose();
            mmvs = null;
            mmf.Dispose();
            mmf = null;
        }
    }    
}
