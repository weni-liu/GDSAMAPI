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
using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;



namespace DLAdjApi.Controllers
{
    [Route("/[controller]")]
    public class M_FilesController : Controller
    {
        private readonly IOptions<DirectorySettings> _directorySettings;   
        public M_FilesController(IOptions<DirectorySettings> directorySettings)
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
                
/*
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return Directory.GetFiles(_directorySettings.Value.Root);
        }
*/
/*
        /// <summary>
        /// 查詢 檔案數量
        /// </summary>
        /// <remarks>
        /// </remarks>                
        /// <param name="shop">廠區</param>
        /// <param name="modelNo">Model No</param>
        /// <param name="type">Training/Testing/Labeling</param>
        /// <param name="jobNo">Job No</param>
        /// <param name="testAns">是否有解答</param>
        /// <returns></returns> 
        [HttpGet("{shop}")]
        public IActionResult Get(string shop, [FromQuery]string modelNo, [FromQuery]string type, [FromQuery]string jobNo, [FromQuery]bool testAns=false) 
        {            
            Int32 folderCount = 0;
            Int32 fileCount = 0;
            string msg = "";
            try
            {
                jobNo = (jobNo==null) ? "" : jobNo;                
                if (shop.Trim() == "" || modelNo.Trim() == "" || type.Trim()== "")
                {
                    return new JsonResult(new BadRequestObjectResult("Parameter Missing!!"));
                }

                //指定路徑
                string path = "";
                if (type == JobInfo.Type.Model.ToString())
                {
                    path = Path.Combine(_directorySettings.Value.Root, shop, modelNo);
                }
                else
                {
                    path = Path.Combine(_directorySettings.Value.Root, shop, modelNo, type );
                }

                if (type == JobInfo.Type.Testing.ToString() && jobNo != "")
                {
                    path = Path.Combine(path, jobNo);
                    if (testAns == true)
                    {
                        path = Path.Combine(path, "Ans");
                    }
                    else
                    {
                        path = Path.Combine(path, "Pic");
                    }
                }
                else if ( type == JobInfo.Type.Labeling.ToString() && jobNo != "")
                {
                    path = Path.Combine(path, jobNo);
                    if (testAns == true)
                    {
                        path = Path.Combine(path, "Label");
                    }
                    else
                    {
                        path = Path.Combine(path, "Pic");
                    }
                }                

                //查詢檔案或目錄數 (0:指定目錄裡無檔案, -1:指定目錄不存在, -2:指定目錄所屬的根目錄不存在, -9:異常)
                DirectoryInfo di = new DirectoryInfo(path);
                if (di.Exists == false)
                {
                    folderCount = -1;
                    fileCount = -1;
                    msg = "指定目錄不存在";
                    if (di.Root.Exists == false)
                    {
                        folderCount = -2;
                        fileCount = -2;
                        msg = "指定目錄不存在, 且根目錄也不存在";
                    }
                }
                else
                {
                    folderCount = di.GetDirectories().Count();
                    fileCount = di.GetFiles("*.*", SearchOption.AllDirectories).Count();
                }
                return Ok(new { fileCount, folderCount, msg });
            }
            catch(Exception ex)
            {
                folderCount = -9;
                fileCount = -9;
                msg = ex.Message;
                return Ok(new { fileCount, folderCount, msg });      //return 0
                //return new JsonResult( new BadRequestObjectResult(ex.Message));
            }
        }
        */
/*
        /// <summary>
        /// 下載 模型壓縮檔案
        /// </summary>
        /// <remarks>
        /// </remarks>                
        /// <param name="modelNo">Model No</param>
        /// <param name="shop">廠區</param>
        /// <param name="type">Model/ExportModel</param>
        /// <param name="volume">壓縮檔有分割, 指定第幾個檔案</param>
        /// <returns></returns> 
        [HttpGet("{modelNo}")]
        public Stream Get(long modelNo, [FromQuery]string shop, [FromQuery]string type="Model", [FromQuery]int volume=0)
        {
            try
            {
                string filePath = "";
                if (type.Trim().ToLower() == "exportmodel")
                {
                    if (volume == 0)
                    {
                        filePath = Path.Combine(_directorySettings.Value.Root, shop, modelNo.ToString(), (modelNo.ToString() + "_Export.zip") );
                    }
                    else
                    {
                        filePath = Path.Combine(_directorySettings.Value.Root, shop, modelNo.ToString(), (modelNo.ToString() + "_Export.zip." + volume.ToString().PadLeft(3, '0')) );
                    }                    
                }
                else
                {
                    filePath = Path.Combine(_directorySettings.Value.Root, shop, modelNo.ToString(),  "Model.zip" );
                }
                FileInfo fi = new FileInfo(filePath);
                if (!fi.Exists)
                {
                    throw new FileNotFoundException("File not found. ", filePath);
                }
                FileStream stream = new FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
                return stream;
            }
            catch(Exception)
            {
                return null;
            }
        }
*/        
     
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
        [HttpPost("UploadFiles")]        
        //[DisableRequestSizeLimit]  //或者取消大小的限制
        public async Task<IActionResult> Post([FromForm]string imageinfo)     //Post(string modelName)  
        {
            //string InputPath = @"D:\SAM\input\";
            //string InputPath = $"{config.GetSection("DirectorySettings:Root").Value}input";
            //string OutputPath = "http:\\\\169.254.82.11\\sam_api\\sam_output";
            DateTime now0 = DateTime.Now;
            try
            {

                HttpContext context = HttpContext;

                // 获取呼叫端Web API的URL
                string apiUrl = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}";
                //Console.WriteLine($"{apiUrl}");

                string targetSubstring = "M_Files/UploadFiles";

                int index = apiUrl.IndexOf(targetSubstring);
                string _url = string.Empty;

                if (index >= 0)
                {
                    _url = apiUrl.Substring(0, index);
                    //Console.WriteLine($"Extracted Part: {_url}");
                }
                else
                {
                    Console.WriteLine("Target substring not found in input string.");
                }



                //string apiUrl = request.RequestUri.ToString();
                var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json");

                var config = builder.Build();
                string InputPath = $"{config.GetSection("DirectorySettings:Root").Value}input/";

                int existingTxtFiles = Directory.EnumerateFiles( InputPath, "*.txt").Count(); // 檔案中txt個數
                if (existingTxtFiles>10)
                {return Ok(new { Message = "Please wait: SAM Model busying" });}
                var files = Request.Form.Files;
                foreach (var formFile in files)
                {
                    //特殊檔案不上傳
                    if (formFile.FileName.ToLower() == "thumbs.db")
                    {
                        continue;
                    }
                    //上傳檔案
                    if (formFile.Length > 0)
                    {                       
                        using (var stream = new FileStream(Path.Combine( InputPath,  formFile.FileName ), FileMode.Create))                        
                        {
                            await formFile.CopyToAsync(stream);
                        }                                                
                    }
                }
                
                ImageInfoData imageInfoData = JsonConvert.DeserializeObject<ImageInfoData>(imageinfo);
                string txtData = "";
                string shop = imageInfoData.Shop;
                string resultType = imageInfoData.ResultType;
                //Console.WriteLine($"Shop: {imageInfoData.Images}");
                foreach (var imageInfo in imageInfoData.Images)
                {
                    string pd_type = imageInfo.Function;
                    string pd_image = imageInfo.ImageName;
                    txtData += $"{pd_image},";
                    if (imageInfo.Labels != null)
                    {
                        foreach (var labelInfo in imageInfo.Labels)
                        {
                        //txtData += labelInfo;
                        txtData += $"{labelInfo.X1},{labelInfo.X2},{labelInfo.Y1},{labelInfo.Y2},{labelInfo.X},{labelInfo.Y},{labelInfo.Label},"; // x1, x2, y1, y2
                        }
                        if (resultType != "Contour")
                        {
                            txtData += "output_image";
                        }
                        txtData += Environment.NewLine;
                    }
                }
                DateTime now = DateTime.Now;
                string formattedDateTime = now.ToString("yyyyMMddHHmmssff");

                //Configuration.GetSection("DirectorySettings:Root").Value


                string filePath = $"{config.GetSection("DirectorySettings:Root").Value}input\\{shop}_{formattedDateTime}.txt"; //txt 儲存路徑 廠區_流水號
                string jsonPath = $"{config.GetSection("DirectorySettings:Root").Value}output\\{shop}_{formattedDateTime}.json";
                //System.IO.File.Move(filePath, txtData);
                System.IO.File.WriteAllText(filePath, txtData); // 儲存txt
                DateTime startTime = DateTime.Now;
                int timeoutInSeconds = 10;
                while (DateTime.Now - startTime < TimeSpan.FromSeconds(timeoutInSeconds))
                {
                    if (System.IO.File.Exists(jsonPath))
                    {   
                        try
                        {
                            // Step 1: 读取JSON文件内容
                            string _Content = System.IO.File.ReadAllText(jsonPath);
                            string _newContent = _Content.Replace(@"D:\\SAM\\output_url\\", $"{_url}Uploads/");
                            System.IO.File.WriteAllText(jsonPath, _newContent);

                            using (FileStream fs = new FileStream(jsonPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            using (StreamReader sr = new StreamReader(fs))
                            {
                                string jsonContent = sr.ReadToEnd();
                                //Console.WriteLine(jsonContent);
                                JToken jsonToken = JToken.Parse(jsonContent);
                                //JObject jsonToken = JObject.Parse(jsonPath);
                                //Console.WriteLine(jsonContent);
                                //string jsonOutput = jsonToken.ToString(Newtonsoft.Json.Formatting.Indented);
                                //System.IO.File.Delete(jsonPath);
                                return Ok(new { Json_data = jsonToken });
                            }
                            //string jsonContent = System.IO.File.ReadAllText(jsonPath);
                            //JObject jsonObject = JObject.Parse(jsonContent);
                            // 刪除檔案?
                            //return Ok(new { Json_data = jsonContent});
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
                return Ok(new { Message = "JSON file not found within the specified time. Check SAM model is alive" });
                
            }
            catch(Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred: " + ex.Message });
            }
        }

        
        [HttpPost("UploadFiles2")]
        public async Task<IActionResult> UploadFiles([Required] List<IFormFile> formFiles, [FromForm] string imageinfo)     //Post(string modelName)  
        {
            //string InputPath = @"D:\SAM\input\";
            //string InputPath = $"{config.GetSection("DirectorySettings:Root").Value}input";
            //string OutputPath = "http:\\\\169.254.82.11\\sam_api\\sam_output";
            DateTime now0 = DateTime.Now;
            try
            {
                HttpContext context = HttpContext;

                // 获取呼叫端Web API的URL
                string apiUrl = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}";
                Console.WriteLine($"{apiUrl}");

                string targetSubstring = "M_Files/UploadFiles2";

                int index = apiUrl.IndexOf(targetSubstring);
                string _url = string.Empty;

                if (index >= 0)
                {
                    _url = apiUrl.Substring(0, index);
                    Console.WriteLine($"Extracted Part: {_url}");
                }
                else
                {
                    Console.WriteLine("Target substring not found in input string.");
                }


                var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json");

                var config = builder.Build();
                string InputPath = $"{config.GetSection("DirectorySettings:Root").Value}input/";

                int existingTxtFiles = Directory.EnumerateFiles(InputPath, "*.txt").Count(); // 檔案中txt個數
                if (existingTxtFiles > 10)
                { return Ok(new { Message = "Please wait: SAM Model busying" }); }
                //var files = Request.Form.Files;
                foreach (var formFile in formFiles)
                {
                    //特殊檔案不上傳
                    if (formFile.FileName.ToLower() == "thumbs.db")
                    {
                        continue;
                    }
                    //上傳檔案
                    if (formFile.Length > 0)
                    {
                        using (var stream = new FileStream(Path.Combine(InputPath, formFile.FileName), FileMode.Create))
                        {
                            await formFile.CopyToAsync(stream);
                        }
                    }
                }

                ImageInfoData imageInfoData = JsonConvert.DeserializeObject<ImageInfoData>(imageinfo);
                string txtData = "";
                string shop = imageInfoData.Shop;
                string resultType = imageInfoData.ResultType;
                //Console.WriteLine($"Shop: {imageInfoData.Images}");
                foreach (var imageInfo in imageInfoData.Images)
                {
                    string pd_type = imageInfo.Function;
                    string pd_image = imageInfo.ImageName;
                    txtData += $"{pd_image},";
                    if (imageInfo.Labels != null)
                    {
                        foreach (var labelInfo in imageInfo.Labels)
                        {
                            //txtData += labelInfo;
                            txtData += $"{labelInfo.X1},{labelInfo.X2},{labelInfo.Y1},{labelInfo.Y2},{labelInfo.X},{labelInfo.Y},{labelInfo.Label},"; // x1, x2, y1, y2
                        }
                        if (resultType != "Contour")
                        {
                            txtData += "output_image";
                        }
                        txtData += Environment.NewLine;
                    }
                }
                DateTime now = DateTime.Now;
                string formattedDateTime = now.ToString("yyyyMMddHHmmssff");




                //Configuration.GetSection("DirectorySettings:Root").Value




                string filePath = $"{config.GetSection("DirectorySettings:Root").Value}input\\{shop}_{formattedDateTime}.txt"; //txt 儲存路徑 廠區_流水號
                string jsonPath = $"{config.GetSection("DirectorySettings:Root").Value}output\\{shop}_{formattedDateTime}.json";
                //System.IO.File.Move(filePath, txtData);
                System.IO.File.WriteAllText(filePath, txtData); // 儲存txt
                DateTime startTime = DateTime.Now;
                int timeoutInSeconds = 10;
                while (DateTime.Now - startTime < TimeSpan.FromSeconds(timeoutInSeconds))
                {
                    if (System.IO.File.Exists(jsonPath))
                    {
                        try
                        {
                            // Step 1: 读取JSON文件内容
                            string _Content = System.IO.File.ReadAllText(jsonPath);
                            string _newContent = _Content.Replace(@"D:\\SAM\\output_url\\", $"{_url}Uploads/");
                            System.IO.File.WriteAllText(jsonPath, _newContent);

                            using (FileStream fs = new FileStream(jsonPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            using (StreamReader sr = new StreamReader(fs))
                            {
                                string jsonContent = sr.ReadToEnd();
                                //Console.WriteLine(jsonContent);
                                JToken jsonToken = JToken.Parse(jsonContent);
                                //JObject jsonToken = JObject.Parse(jsonPath);
                                //Console.WriteLine(jsonContent);
                                //string jsonOutput = jsonToken.ToString(Newtonsoft.Json.Formatting.Indented);
                                //System.IO.File.Delete(jsonPath);
                                return Ok(new { Json_data = jsonToken });
                            }
                            //string jsonContent = System.IO.File.ReadAllText(jsonPath);
                            //JObject jsonObject = JObject.Parse(jsonContent);
                            // 刪除檔案?
                            //return Ok(new { Json_data = jsonContent});
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
                return Ok(new { Message = "JSON file not found within the specified time. Check SAM model is alive" });

            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred: " + ex.Message });
            }
        }
        public class ImageUploadModel
        {
            public IFormFile Image { get; set; }
        }
                /* go let's go yoe !
                jobNo = (jobNo==null) ? "" : jobNo;                
                if (shop.Trim() == "" || modelNo.Trim() == "" || type.Trim()== "")
                {
                    return new JsonResult( new BadRequestObjectResult("Parameter Missing!!"));
                }

                //Create Folder                
                var fileName = "";
                var path = "";
                if (type.Trim().ToLower() == "importmodel")
                {
                    path = Path.Combine(_directorySettings.Value.Root, shop, modelNo);
                }
                else
                {
                    path = Path.Combine(_directorySettings.Value.Root, shop, modelNo, type);
                    //指定圖片的目錄
                    if (folder.Trim() != "")
                    {
                        path = Path.Combine(path, folder);
                    }
                }
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                if ((type == JobInfo.Type.Testing.ToString() || type == JobInfo.Type.Labeling.ToString()) && jobNo != "")
                {
                    //Create Folder
                    path = Path.Combine(path, jobNo);
                    if (!Directory.Exists(Path.Combine(path, "Pic")))
                    {
                        Directory.CreateDirectory(Path.Combine(path,"Pic"));
                    }   
                    if (!Directory.Exists(Path.Combine(path, "Ans")))
                    {
                        Directory.CreateDirectory(Path.Combine(path,"Ans"));
                    }                  
                    if (type == JobInfo.Type.Labeling.ToString())
                    {
                        //存放 Labeling 產出的 Label 檔案
                        if (!Directory.Exists(Path.Combine(path, "Label")))
                        {
                            Directory.CreateDirectory(Path.Combine(path,"Label"));
                        }       
                    }      

                    //是否有解答
                    if (testAns == true)
                    {
                        path = Path.Combine(path, "Ans");
                        fileName = "Testing.csv";
                    }
                    else
                    {
                        path = Path.Combine(path, "Pic");
                    }  
                }
                //else if (type.Trim().ToLower() == "importmodel")
                //{
                //    fileName = modelNo + "_Import.zip";
                //}
                
                //Upload Files
                foreach (var formFile in files)
                {
                    //特殊檔案不上傳
                    if (formFile.FileName.ToLower() == "thumbs.db")
                    {
                        continue;
                    }

                    //上傳檔案
                    if (formFile.Length > 0)
                    {                       
                        //using (var stream = new FileStream(Path.Combine(path, ((fileName=="") ? formFile.FileName : fileName)), FileMode.OpenOrCreate))
                        using (var stream = new FileStream(Path.Combine(path, ((fileName=="") ? formFile.FileName : fileName)), FileMode.Create))                        
                        {
                            await formFile.CopyToAsync(stream);
                        }                                                
                    }
                }
                */

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
                        Directory.Delete(path, true);
                    }
                }
                return new OkResult();
            }
            catch(Exception ex)
            {
                return new JsonResult(new BadRequestObjectResult(ex.Message));
            }
        }
/*
//        [Authorize]
        /// <summary>
        /// 移動 檔案
        /// </summary>
        /// <remarks>
        /// </remarks>                
        /// <param name="shop">廠區(必)</param>
        /// <param name="modelNo">Model No</param>
        /// <param name="type">Testing/Labeling</param>
        /// <param name="jobNo">Job No</param>
        /// <param name="testAns">是否有解答</param>
        /// <returns></returns> 
        [HttpPost("MoveToSam")]
        public IActionResult Post(string shop, [FromQuery]string modelNo, [FromQuery]string type, [FromQuery]string jobNo, [FromQuery]bool testAns=false)
        {   
            // 確認SAM執行序是否超過10
            string sourceFilePath = @"C:\Users\yangu.chen\TodoApi0\13.txt"; // 要移動的檔案完整路徑
            string targetFolderPath = @"C:\Users\yangu.chen\TodoApi"; // 目標資料夾路徑

            // 確保目標資料夾存在
            if (!Directory.Exists(targetFolderPath))
            {   
                // 創立資料夾
                Directory.CreateDirectory(targetFolderPath); 
            }
            
            // 檔案移動
            string targetFilePath = Path.Combine(targetFolderPath, Path.GetFileName(sourceFilePath));
            System.IO.File.Move(sourceFilePath, targetFilePath);

            return Ok("OK");
        }
*/
/*
        [HttpPost("")]
        public IActionResult Post([FromBody] ImageInfoData imageInfoData)
        {
            try
            {
                // 將 JSON 資料整理成 txt 格式
                string txtData = "Shop: " + imageInfoData.Shop + Environment.NewLine;
                txtData += "ModelNo: " + imageInfoData.ModelNo + Environment.NewLine;
                txtData += "Type: " + imageInfoData.Type + Environment.NewLine;

                foreach (var image in imageInfoData.Images)
                {
                    txtData += "ImageName: " + image.ImageName + Environment.NewLine;
                    txtData += "Function: " + image.Function + Environment.NewLine;

                    if (image.Labels != null && image.Labels.Count > 0)
                    {
                        txtData += "Labels: " + Environment.NewLine;
                        foreach (var label in image.Labels)
                        {
                            if (image.Function == "box")
                            {
                                txtData += $"Box: x1={label.X1}, y1={label.Y1}, x2={label.X2}, y2={label.Y2}" + Environment.NewLine;
                            }
                            else if (image.Function == "point")
                            {
                                txtData += $"Point: x={label.X}, y={label.Y}" + Environment.NewLine;
                            }
                            else if (image.Function == "box+point")
                            {
                                txtData += $"Box: x1={label.X1}, y1={label.Y1}, x2={label.X2}, y2={label.Y2}" + Environment.NewLine;
                                txtData += $"Point: x={label.X}, y={label.Y}" + Environment.NewLine;
                            }
                        }
                    }

                    txtData += Environment.NewLine;
                }

                // 將資料寫入 txt 檔案
                string filePath = "C:\\Users\\yangu.chen\\dm\\file.txt"; // 更換成您想要存儲的檔案路徑
                System.IO.File.WriteAllText(filePath, txtData);

                return Ok("資料已成功寫入 txt 檔案。");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        */
    }
}
    public class ImageInfoData
    {
        public string Shop { get; set; }
        public string ModelNo { get; set; }
        public string Type { get; set; }
        public string ResultType { get; set; }
        public List<ImageInfo> Images { get; set; }
    }

    public class ImageInfo
    {
        public string ImageName { get; set; }
        public string Function { get; set; }
        public List<LabelInfo> Labels { get; set; }
    }

    public class LabelInfo
    {
        public int X1 { get; set; }
        public int Y1 { get; set; }
        public int X2 { get; set; }
        public int Y2 { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public string Label { get; set; }
    }