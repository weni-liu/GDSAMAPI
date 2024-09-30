using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using DLAdjApi.Models;
using DLAdjApi.Services;
using Microsoft.Extensions.Options;

namespace DLAdjApi.Controllers
{
    [Route("/[controller]")]
    public class GlobalVariableController : Controller
    {

        public GlobalVariableController()
        {
        }  

        private string GetUserByJwt()
        {
            JwtAPI jwtObj = new JwtAPI(User);            
            string user = jwtObj.GetUserName();       
            return user;
        }        
     
        /// <summary>
        /// 取得 Global 參數裡的資料
        /// </summary>
        /// <remarks>
        /// </remarks>                
        /// <param name="VariableName">參數名稱 : ConvergenceToUmdTime</param>
        /// <returns></returns>    
         [HttpGet("{VariableName}")]
        public IActionResult Get(string VariableName)
        {            
            try
            {   
                GlobalVariables.CheckTimeOut();
                if (GlobalVariables.ConvergenceToUmdTime.Keys.Contains(VariableName) == true)
                {                
                    return new JsonResult (GlobalVariables.ConvergenceToUmdTime[VariableName].ToString());                
                }
                else
                {
                    return new JsonResult ("no data");                
                }
                
            }
            catch(Exception ex)
            {
                return new JsonResult( new BadRequestObjectResult(ex.Message));
            }
        }

        /// <summary>
        /// 新增 Global 參數裡的資料
        /// </summary>
        /// <remarks>
        /// </remarks>                
        /// <param name="VariableName">string</param>
        /// <returns></returns>    
         [HttpPost()]
        public IActionResult Post(string VariableName)
        {            
            try
            {   
               if (GlobalVariables.ConvergenceToUmdTime.Keys.Contains(VariableName) == false)
               {                
                    GlobalVariables.ConvergenceToUmdTime.Add(VariableName, DateTime.Now);
               }
               return new JsonResult(GlobalVariables.ConvergenceToUmdTime[VariableName].ToString());    
            }
            catch(Exception ex)
            {
                return new JsonResult( new BadRequestObjectResult(ex.Message));
            }
        }      
        
          
    }
}