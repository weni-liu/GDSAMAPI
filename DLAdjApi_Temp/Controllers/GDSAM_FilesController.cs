using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using DLAdjApi;
using DLAdjApi.Services;
using DLAdjApi.Models;
using Microsoft.AspNetCore.Authorization;
using System.Xml.Serialization;
using OpenCvSharp;  //dotnet add package OpenCvSharp4.Windows
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;
using System.Threading;
using Newtonsoft.Json.Linq;
using System.Xml;

namespace DLAdjApi.Controllers
{
    [Route("/[controller]")]
    public class GDSAM_FilesController : Controller
    {
        private readonly IOptions<DirectorySettings> _directorySettings;   
        public GDSAM_FilesController(IOptions<DirectorySettings> directorySettings)
        {
            _directorySettings = directorySettings;
            Tools.NetConnection(_directorySettings.Value.Root, _directorySettings.Value.Un, _directorySettings.Value.Pwd);
        }

        private string GetUserByJwt()
        {
            JwtAPI jwtObj = new JwtAPI(User);            
            string user = jwtObj.GetUserName();       
            return user;
        }

     
//        [Authorize]
        /// <summary>
        /// 上傳 檔案
        /// </summary>
        /// <remarks>
        /// UploadFiles
        ///
        ///     指定 Client 端的檔案
        ///
        /// </remarks>                
        /// <param name="ImageInfoData">(必) 廠區</param>
        /// <returns></returns> 
        [HttpPost("UploadImage")]        
        //[DisableRequestSizeLimit]  //或者取消大小的限制
        public async Task<IActionResult> Post([FromForm]string imageinfo) //Post(string modelName)  
        {   
            string uploadPath = "D:\\Autolabeling_tmp\\Images"; // AP server的 'D:\\Uploads\\Autolabeling_input'
            string txtPath = "D:\\Uploads\\Autolabeling_input\\";
            // 確保目錄存在
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }
            if (!Directory.Exists(txtPath))
            {
                Directory.CreateDirectory(txtPath);
            }

            DateTime now0 = DateTime.Now;
            try
            {   
                // 保存圖片
                var files = Request.Form.Files;
                foreach (var file in files)
                {
                    if (file.Length > 0)
                    {
                        string filePath = Path.Combine(uploadPath, file.FileName);
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(fileStream);
                        }
                        
                        DateTime startTime0 = DateTime.Now;
                        int timeoutInSeconds0 = 2;
                        while (DateTime.Now - startTime0 < TimeSpan.FromSeconds(timeoutInSeconds0))
                        {
                            if (System.IO.File.Exists(filePath))
                            {
                                break;
                            }
                        }

                        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file.FileName);
                        // 寫入txt
                        string txtFilePath = Path.Combine(txtPath, fileNameWithoutExtension + ".txt");
                        string txtData = $"D:\\Autolabeling_tmp\\Images\\{file.FileName}";
                        System.IO.File.WriteAllText(txtFilePath, txtData); // 儲存txt
                        DateTime now = DateTime.Now;
                        string jsonPath = $"D:\\Uploads\\Autolabeling_tmp\\Label\\{fileNameWithoutExtension}.json";
                        if (!Directory.Exists("D:\\Uploads\\Autolabeling_tmp\\Label"))
                        {
                            Directory.CreateDirectory("D:\\Uploads\\Autolabeling_tmp\\Label");
                        }
                        DateTime startTime = DateTime.Now;
                        int timeoutInSeconds = 10;
                        while (DateTime.Now - startTime < TimeSpan.FromSeconds(timeoutInSeconds))
                        {
                            if (System.IO.File.Exists(jsonPath))
                            {   
                                try
                                {   
                                    using (FileStream fs = new FileStream(jsonPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                                    using (StreamReader sr = new StreamReader(fs))
                                    {
                                        string jsonContent = sr.ReadToEnd();
                                        // 最後_刪除圖片文件
                                        JToken jsonToken = JToken.Parse(jsonContent);
                                        fs.Dispose();
                                    
                                        if (System.IO.File.Exists(filePath))
                                        {
                                            System.IO.File.Delete(filePath);
                                        }
                                        if (System.IO.File.Exists(jsonPath))
                                        {
                                            System.IO.File.Delete(jsonPath);
                                        }
                                        return Ok(new { Json_data = jsonToken });
                                    }
                                    
                                }
                                catch (Exception ex)
                                {
                                    return Ok(new { message = ex.Message });
                                    //Console.WriteLine("讀取 JSON 檔案時發生錯誤：" + ex.Message);
                                }
                                //break;
                            }
                            Thread.Sleep(100);
                        }

                    }
                }
                return Ok(new { Message = $"Jason file not found within the specified time." });
                
            }
            catch(Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred: " + ex.Message });
            }
        }


        public class ImageUploadModel
        {
            public IFormFile Image { get; set; }
        }

//        [Authorize]
        /// <summary>
        /// 刪除 檔案
        /// </summary>
        /// <remarks>
        /// </remarks>                
        /// <param name="shop">廠區</param>
        /// <param name="modelNo">Model No</param>
        /// <param name="type">Testing/Labeling</param>
        /// <param name="jobNo">Job No</param>
        /// <param name="testAns">是否有解答</param>
        /// <returns></returns>
        [HttpPost("GroundingdinoImage")]        
        //[DisableRequestSizeLimit]  //或者取消大小的限制
        public async Task<IActionResult> PostGroundingdinoImage([FromForm]string imageinfo) //Post(string modelName)  
        {   
            string uploadPath = "D:\\Autolabeling_tmp\\Groundingdino_Images"; // AP server的 'D:\\Uploads\\Autolabeling_input'
            string txtPath = "D:\\Uploads\\Groundingdino_input\\";
            // 確保目錄存在
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }
            if (!Directory.Exists(txtPath))
            {
                Directory.CreateDirectory(txtPath);
            }

            DateTime now0 = DateTime.Now;
            try
            {   
                // 保存圖片
                var files = Request.Form.Files;
                foreach (var file in files)
                {
                    if (file.Length > 0)
                    {
                        string filePath = Path.Combine(uploadPath, file.FileName);
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(fileStream);
                        }
                        
                        DateTime startTime0 = DateTime.Now;
                        int timeoutInSeconds0 = 2;
                        while (DateTime.Now - startTime0 < TimeSpan.FromSeconds(timeoutInSeconds0))
                        {
                            if (System.IO.File.Exists(filePath))
                            {
                                break;
                            }
                        }

                        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file.FileName);
                        // 寫入txt
                        string txtFilePath = Path.Combine(txtPath, fileNameWithoutExtension + ".txt");
                        string txtData = $"D:\\Autolabeling_tmp\\Groundingdino_Images\\{file.FileName}";
                        System.IO.File.WriteAllText(txtFilePath, txtData); // 儲存txt
                        DateTime now = DateTime.Now;
                        string xmlPath = $"D:\\Uploads\\Autolabeling_tmp\\Groundingdino_Label\\{fileNameWithoutExtension}.xml";
                        if (!Directory.Exists("D:\\Uploads\\Autolabeling_tmp\\Groundingdino_Label"))
                        {
                            Directory.CreateDirectory("D:\\Uploads\\Autolabeling_tmp\\Groundingdino_Label");
                        }
                        DateTime startTime = DateTime.Now;
                        int timeoutInSeconds = 10;
                        while (DateTime.Now - startTime < TimeSpan.FromSeconds(timeoutInSeconds))
                        {
                            if (System.IO.File.Exists(xmlPath))
                            {   
                                try
                                {   
                                    XmlDocument doc = new XmlDocument();
                                    doc.Load(xmlPath);
                                    string xmlContent = doc.OuterXml;
                                    {
                                        //string jsonContent = sr.ReadToEnd();
                                        // 最後_刪除圖片文件
                                        //JToken jsonToken = JToken.Parse(jsonContent);
                                        //fs.Dispose();
                                    
                                        if (System.IO.File.Exists(filePath))
                                        {
                                            System.IO.File.Delete(filePath);
                                        }
                                        if (System.IO.File.Exists(xmlPath))
                                        {
                                            System.IO.File.Delete(xmlPath);
                                        }
                                        return Ok(new { Xml_data = xmlContent });
                                    }
                                    
                                }
                                catch (Exception ex)
                                {
                                    return Ok(new { message = ex.Message });
                                    //Console.WriteLine("讀取 JSON 檔案時發生錯誤：" + ex.Message);
                                }
                                //break;
                            }
                            Thread.Sleep(100);
                        }

                    }
                }
                return Ok(new { Message = $"Jason file not found within the specified time." });
                
            }
            catch(Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred: " + ex.Message });
            }
        }

        [HttpDelete("{shop}")]
        public IActionResult Delete(string shop, [FromQuery]string modelNo, [FromQuery]string type, [FromQuery]string jobNo, [FromQuery]bool testAns=false)
        {
            try
            {
                jobNo = (jobNo==null) ? "" : jobNo;
                if (shop.Trim() == "" || modelNo.Trim() == "" || type.Trim()== "")
                {
                    return new JsonResult( new BadRequestObjectResult("Parameter Missing!!"));
                }

                //Delete Folder
                var path = Path.Combine(_directorySettings.Value.Root, shop, modelNo, type );
                if ((type == JobInfo.Type.Testing.ToString() || type==JobInfo.Type.Labeling.ToString()) && jobNo != "")
                {
                    if (testAns == true)
                    {
                        path = Path.Combine(path, jobNo, "Ans");
                        if (Directory.Exists(path))
                        {
                            Directory.Delete(path, true);
                        }
                    }
                    else
                    {
                        path = Path.Combine(path, jobNo, "Pic");
                        if (Directory.Exists(path))
                        {
                            Directory.Delete(path, true);
                        }
                    }
                }
                else if (type != JobInfo.Type.Testing.ToString())
                {
                    if (Directory.Exists(path))
                    {
                        Directory.Delete(path,true);
                    }
                }
                return new OkResult();
                
            }
            catch(Exception ex)
            {
                return new JsonResult(new BadRequestObjectResult(ex.Message));
            }
        }

    }
}
   