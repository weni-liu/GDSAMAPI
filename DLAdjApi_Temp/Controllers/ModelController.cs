using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DLAdjApi.Models;
using DLAdjApi.Services;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using log4net;
using System.Xml;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace DLAdjApi.Controllers
{

    [Route("/[controller]")]
    public class ModelController : Controller
    {
        private readonly IModelService _modelService;
        private readonly IUrlHelper _url;
        private readonly IOptions<DirectorySettings> _directorySettings;
        private readonly IOptions<ShopSettings> _shopSettings;
        //private readonly ILogger _log;
        private readonly ILog _log;        
        
        public ModelController(IModelService modelService, IUrlHelper url, IOptions<DirectorySettings> directorySettings, IOptions<ShopSettings> shopSettings) //public ModelController(IModelService modelService, IOptions<DirectorySettings> directorySettings, ILogger<ModelController> log)
        {
            this._modelService = modelService;
            _url = url;
            _directorySettings = directorySettings;
            _shopSettings = shopSettings;          
            //_log = log
            //_log = Startup.log;   
            _log = LogManager.GetLogger(Startup.repo.Name, typeof(ModelController));
             
        }

        private string GetUserByJwt()
        {
            JwtAPI jwtObj = new JwtAPI(User);            
            string user = jwtObj.GetUserName();       
            return user;
        } 



        /// <summary>
        /// 查詢 Model 資料
        /// </summary>
        /// <remarks>
        /// </remarks>                
        /// <param name="seq">Model No</param>
        /// <returns></returns>   
        [HttpGet("{seq}")]
        public IActionResult Get(long seq)
        {
            try
            {
                _log.Info("Get Model(" + seq.ToString() + ")");           
                var model = _modelService.Get(seq);

                if (model == null)
                {
                    return new JsonResult(new BadRequestObjectResult("Not Found!"));        //"Not Found!" Release AP程式有拿來判斷
                }
                //_log.LogInformation("Get Model");        
                //_log.Info("Get Model..");
                return new JsonResult(model);
            }
            catch (Exception ex)
            {
                return new JsonResult(new BadRequestObjectResult(ex.Message));
            }
        }

        /// <summary>
        /// 新增 Model 資料
        /// </summary>
        /// <remarks>
        /// </remarks>                
        /// <param name="model">ModelData Object
        ///
        /// 不含 Array / Object 相關的參數，但 Parameter 除外，Parameter 可以包在 ModelData 裡，
        /// 新增 Model 時，若 ModelData 包含 Parameter 資料，就會同步新增 Parameter.
        ///</param>
        /// <returns></returns> 
        //[Authorize(AuthenticationSchemes = "inx-jwt")]
        [Authorize]
        [HttpPost()]
        public IActionResult Post([FromBody]ModelData model)
        {
            try
            {                
                _log.Info("Add Model");                 
                _modelService.Add(model, _directorySettings);
                return new JsonResult(model);
            }
            catch (Exception ex)
            {
                return new JsonResult(new BadRequestObjectResult(ex.Message));
            }
        }

        /// <summary>
        /// 修改 Model 資料 (No Authorize)
        /// </summary>
        /// <remarks>
        /// </remarks>                
        /// <param name="model">ModelData Object
        ///
        /// 不含 Array / Object 相關的參數，但 Parameter 除外，Parameter 可以包在 ModelData 裡，
        /// 修改 Model 時，若 ModelData 包含 Parameter 資料，就會同步修改 Parameter.
        /// </param>
        /// <returns></returns> 
        //[Authorize]   //因後端AP有使用Update Model, 故不驗證
        [HttpPut("")]
        public IActionResult Put([FromBody]ModelData model)
        {
            try
            {
                if (model.ModelType != null)
                {
                    _log.Info("Update Model (" + model.ModelNo + " : " + model.ModelType + ")");
                }
                else
                {
                    _log.Info("Update Model (" + model.ModelNo + ")");
                }
                
                //_modelService.CheckUserAuthority(model.Shop,  GetUserByJwt(), AuthorityInfo.ModelModify, true);   
                _modelService.Update(model, _url, _directorySettings, _shopSettings);
                return new JsonResult(new OkObjectResult(""));     //For Web Update Model 判斷使用
            }
            catch (Exception ex)
            {
                return new JsonResult(new BadRequestObjectResult(ex.Message));
            }
        }

        /// <summary>
        /// 刪除 Model 資料 (No Authorize)
        /// </summary>
        /// <remarks>
        /// </remarks>                
        /// <param name="modelNo">Model No</param>
        /// <returns></returns> 
        //[Authorize]   //因後端AP有使用Delete Model, 故不驗證
        [HttpDelete("{modelNo}")]
        public IActionResult Delete(long modelNo)
        {
            try
            {
                _log.Info("Delete Model"); 
                //ModelData md = _modelService.Get(modelNo);
                //_modelService.CheckUserAuthority(md.Shop,  GetUserByJwt(), AuthorityInfo.ModelModify, true);  
                _modelService.Delete(modelNo, _directorySettings);
                return new OkResult();
            }
            catch (Exception ex)
            {
                return new JsonResult(new BadRequestObjectResult(ex.Message));
                //return new NotFoundObjectResult("錯誤！Model 不存在.");
            }
        }

    }
}