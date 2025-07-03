這段程式碼是一個用於處理 AI 自動標註系統的 ASP.NET Core Web API 控制器。讓我詳細說明其功能和環境設定需求：

## 程式碼功能說明

### 1. 控制器基本結構
````csharp
[Route("/[controller]")]
public class GDSAM_FilesController : Controller
{
    private readonly IOptions<DirectorySettings> _directorySettings;
    
    public GDSAM_FilesController(IOptions<DirectorySettings> directorySettings)
    {
        _directorySettings = directorySettings;
        Tools.NetConnection(_directorySettings.Value.Root, _directorySettings.Value.Un, _directorySettings.Value.Pwd);
    }
}
````

**功能：**
- 管理 GroundingDINO + SAM 自動標註系統的檔案上傳
- 透過依賴注入取得目錄設定
- 建立網路連線（可能是連接到共享資料夾）

### 2. JWT 身份驗證
````csharp
private string GetUserByJwt()
{
    JwtAPI jwtObj = new JwtAPI(User);            
    string user = jwtObj.GetUserName();       
    return user;
}
````

**功能：**
- 從 JWT Token 中提取使用者資訊
- 提供身份驗證支援

### 3. 第一個 API：UploadImage (SAM 處理)
````csharp
[HttpPost("UploadImage")]
[Authorize]
[DisableRequestSizeLimit]
public async Task<IActionResult> Post([FromForm]string imageinfo)
````

**處理流程：**
```
1. 上傳圖片 → D:\Autolabeling_tmp\Images\
2. 建立控制檔 → D:\Uploads\Autolabeling_input\{filename}.txt
3. 等待 AI 處理 (10秒內)
4. 讀取結果 → D:\Uploads\Autolabeling_tmp\Label\{filename}.json
5. 回傳 JSON 格式的分割結果
6. 清理暫存檔案
```

### 4. 第二個 API：GroundingdinoImage (GroundingDINO 處理)
````csharp
[HttpPost("GroundingdinoImage")]
[Authorize]
[DisableRequestSizeLimit]
public async Task<IActionResult> PostGroundingdinoImage([FromForm]string imageinfo)
````

**處理流程：**
```
1. 上傳圖片 → D:\Autolabeling_tmp\Groundingdino_Images\
2. 建立控制檔 → D:\Uploads\Groundingdino_input\{filename}.txt
3. 等待 AI 處理 (10秒內)
4. 讀取結果 → D:\Uploads\Autolabeling_tmp\Groundingdino_Label\{filename}.xml
5. 回傳 XML 格式的檢測結果
6. 清理暫存檔案
```

### 5. 刪除功能
````csharp
[HttpDelete("{shop}")]
public IActionResult Delete(string shop, [FromQuery]string modelNo, [FromQuery]string type, [FromQuery]string jobNo, [FromQuery]bool testAns=false)
````

**功能：**
- 根據參數刪除指定的資料夾
- 支援不同類型的資料清理（Testing/Labeling/Ans/Pic）

## 環境設定需求

### 1. NuGet 套件安裝
````bash
# 必要套件
dotnet add package Microsoft.AspNetCore.Mvc
dotnet add package Microsoft.Extensions.Options
dotnet add package OpenCvSharp4.Windows
dotnet add package Newtonsoft.Json
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
````

### 2. 建立必要的模型類別

#### DirectorySettings.cs
````csharp
namespace DLAdjApi.Models
{
    public class DirectorySettings
    {
        public string Root { get; set; }
        public string Un { get; set; }      // Username
        public string Pwd { get; set; }     // Password
    }
}
````

#### JobInfo.cs
````csharp
namespace DLAdjApi.Models
{
    public class JobInfo
    {
        public enum Type
        {
            Testing,
            Labeling
        }
    }
}
````

#### JwtAPI.cs
````csharp
using System.Security.Claims;

namespace DLAdjApi.Services
{
    public class JwtAPI
    {
        private readonly ClaimsPrincipal _user;
        
        public JwtAPI(ClaimsPrincipal user)
        {
            _user = user;
        }
        
        public string GetUserName()
        {
            return _user?.Identity?.Name ?? "Anonymous";
        }
    }
}
````

#### Tools.cs
````csharp
namespace DLAdjApi
{
    public static class Tools
    {
        public static void NetConnection(string root, string username, string password)
        {
            // 實作網路連線邏輯
            // 例如：連接到網路共享資料夾
            Console.WriteLine($"Connecting to {root} with user {username}");
        }
    }
}
````

### 3. appsettings.json 設定
````json
{
  "DirectorySettings": {
    "Root": "\\\\server\\share\\path",
    "Un": "your_username",
    "Pwd": "your_password"
  },
  "Jwt": {
    "Key": "your_jwt_secret_key_here_must_be_long_enough",
    "Issuer": "your_issuer",
    "Audience": "your_audience"
  }
}
````

### 4. Program.cs 依賴注入設定
````csharp
var builder = WebApplication.CreateBuilder(args);

// 設定 DirectorySettings
builder.Services.Configure<DirectorySettings>(
    builder.Configuration.GetSection("DirectorySettings"));

// JWT 驗證設定
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

builder.Services.AddControllers();

var app = builder.Build();

// 啟用驗證和授權
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
````

### 5. 資料夾結構建立
````bash
# 建立必要的資料夾
mkdir "D:\Uploads\Autolabeling_input"
mkdir "D:\Uploads\Groundingdino_input"
mkdir "D:\Uploads\Autolabeling_tmp\Label"
mkdir "D:\Uploads\Autolabeling_tmp\Groundingdino_Label"
mkdir "D:\Autolabeling_tmp\Images"
mkdir "D:\Autolabeling_tmp\Groundingdino_Images"
````

### 6. 權限設定
- 確保 IIS 或應用程式有讀寫上述資料夾的權限
- 如果使用網路共享，確保網路連線正常

### 7. AI 處理服務
這個 API 依賴於背景運行的 AI 處理服務：
- **SAM 服務**：監控 `D:\Uploads\Autolabeling_input\` 產生 JSON 結果
- **GroundingDINO 服務**：監控 `D:\Uploads\Groundingdino_input\` 產生 XML 結果

### 8. 測試 API
````bash
# 測試 SAM 處理
curl -X POST "https://localhost:5001/GDSAM_Files/UploadImage" \
     -H "Authorization: Bearer your_jwt_token" \
     -F "imageinfo=test" \
     -F "file=@test_image.jpg"

# 測試 GroundingDINO 處理
curl -X POST "https://localhost:5001/GDSAM_Files/GroundingdinoImage" \
     -H "Authorization: Bearer your_jwt_token" \
     -F "imageinfo=test" \
     -F "file=@test_image.jpg"
````

這個系統設計為與 AI 處理服務協同工作的 API 閘道，負責檔案管理和結果回傳。

Similar code found with 1 license type
