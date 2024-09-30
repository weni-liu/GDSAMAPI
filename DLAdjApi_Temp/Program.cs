using System.IO;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using System.Xml;
using System.Reflection;
using System.Diagnostics;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using log4net;
using System.Threading;
using System.IO.MemoryMappedFiles;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using System.Net;

namespace DLAdjApi
{
   
    public class Program
    {
        public static RsaJwk.RsaJwk Jwk;
        public static ILog log {get;set;}

        public static void Main(string[] args)
        {            
            //Log                                                
            XmlDocument log4netConfig = new XmlDocument();
            log4netConfig.Load(File.OpenRead(@".\log4net.config"));
            var repo = log4net.LogManager.CreateRepository(Assembly.GetEntryAssembly(), typeof(log4net.Repository.Hierarchy.Hierarchy));
            log4net.Config.XmlConfigurator.Configure(repo, log4netConfig["log4net"]);
            log = log4net.LogManager.GetLogger(typeof(Program));            
            log.Info("API Start");            

            //Initital
            ServicePointManager.DefaultConnectionLimit = 20;   //HttpClient 同時的connection數

            //Run
            Jwk = JsonConvert.DeserializeObject<RsaJwk.RsaJwk>(
            File.ReadAllText(Directory.GetCurrentDirectory() + "/App_Data/keys/public.jwk_2048.json"));            
            Console.WriteLine("");
            Console.WriteLine("******* Please Don't Close the Console *******");
            Console.WriteLine("");
            Console.WriteLine("[ SAM API Service ]");
            Console.WriteLine("");
            Console.WriteLine("Version : " + ApInfo.Version);
            Console.WriteLine("Start Date : " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json");

            var config = builder.Build();
            string[] defaultArgs = new string[]{ "http://*:8080" };
            //string[] defaultArgs = new string[1]
                ;
            if (config.GetSection("UseUrls").Value != null)
            {
                Console.WriteLine($"Url:{config.GetSection("UseUrls").Value}");
                defaultArgs[0] = $"{config.GetSection("UseUrls").Value}";
            }

            CreateWebHostBuilder(defaultArgs).Build().Run();   //BuildWebHost(args).Run();                 

            //End
            LifeguardThread.close = true;            
            log.Info("API End");         
            Console.WriteLine("");   
            Console.WriteLine("(" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ")");
                     
        }

        //public static IWebHost BuildWebHost(string[] args) =>            
        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>                    
            WebHost.CreateDefaultBuilder(args)
                //.UseUrls("http://*:5000;http://localhost:5001")
                .UseUrls(args[0])             // online to change 80 port !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!       
                .UseStartup<Startup>()
                //.UseKestrel(options =>
                .ConfigureKestrel((context, options) =>
                {
                    //options.AddServerHeader = false; 
                    //options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(3);      //HTTP持久連線的時間。(預設 2 分鐘) 
                    //options.Limits.MaxConcurrentConnections = 100;                  //同時連線數限制。(預設無限)
                    //options.Limits.MaxConcurrentUpgradedConnections = 100;          //同時連線數限制，包含如 WebSockets 等，其他非連線方式 HTTP。(預設無限)
                    options.Limits.MaxRequestBodySize = 4147483648;                  //Request 封包限制。(預設 30,000,000 bytes 約 28.6MB) ; options.Limits.MaxRequestBodySize = null;   //所有controller都不限制post的body大小
                    options.Limits.MinRequestBodyDataRate = new MinDataRate(bytesPerSecond: 240, gracePeriod: TimeSpan.FromSeconds(10));    //Request 傳送速率若低於每秒 N bytes，連續 Y 秒，則視為連線逾時。(預設連續 5 秒低於 240 bytes為連線逾時)
                    options.Limits.MinResponseDataRate = new MinDataRate(bytesPerSecond: 240, gracePeriod: TimeSpan.FromSeconds(10));       //Response 傳送速率若低於每秒 N bytes，連續 Y 秒，則視為連線逾時。(預設連續 5 秒低於 240 bytes為連線逾時)
                    //options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(5); //Server 處理一個封包最長的時間。(預設 30 秒)
                });    
                //.Build();              

            //web.config 加入 requestTimeout 可以避免 Server 執行太久, 導致前端 502 異常
            //<aspNetCore processPath="dotnet" arguments=".\DLAdjApi.dll" stdoutLogEnabled="false" stdoutLogFile=".\logs\stdout" requestTimeout="00:30:00"/>
   


    }

}
