
# Project Title

提供AiAdj Web, Model Server,及INX Adj所需API

## Getting Started

### Prerequisites

*  安裝visual studio code

* * *
### Installing

Press F5 (Start Debugging),系統會自動進行nuget Package Restore;若nuget無法下載Package時,請修改nuget.config.
*  開啟%AppData%\nuget\nuget.config, 在"packageSources"區段加入以下內容，此為內部nuget server

```
  <packageSources>
    <add key="nuget.org" value="http://10.53.56.79:5000/v3/index.json" />
  </packageSources>
```
* * *
## Running the tests
> 如何實作存取DB資料的API?

1. Models新增一個TestData.cs


```
using System;

namespace DLAdjApi.Models
{
    public class TestData
    {
        public int no {get; set;}
        public string name {get; set;}

    }
}
```


2. Models新增DLAdjContext.cs(DB對應的teble)
   
```
 modelBuilder.Entity<TestData>(entity =>
            {
                entity.HasKey(e =>  new {e.no} );                
                entity.Property(e => e.no).IsRequired().HasMaxLength(2);
                entity.Property(e => e.name).IsRequired().HasMaxLength(10);
               
            });  

```

3. MySql新增TestData table
3.1. Table名稱須與modelBuilder.Entity < TestData > 相同
![Alt text](./image/TestDataTable.PNG "Optional title")
3.2. 欄位設定:
![Alt text](./image/TestDataTableColumnsType.PNG"Optional title")

4. 在Models->BaseDLAdjContext.cs,新增一個DB Context

```
namespace DLAdjApi.Models
{
    public abstract class BaseDLAdjContext : IdentityDbContext
    {       
        public virtual DbSet<TestData> TestData { get; set; }
    }

}
```

5. Services-->BaseModelService.cs,建立撈DB的service

```
  public IEnumerable<TestData>  GetTestData()
        {
            return Context.TestData.Where(m=>m.no == 0).ToList();    
        }   

```

6. Services-->IModelService.cs建位GetTestData Interface

```
using DLAdjApi.Models;

namespace DLAdjApi.Services
{
    public interface IModelService
    {   
        IEnumerable<TestData>  GetTestData();
    }
}
```

7. Controllers新增TestController.cs,實作一個GetTestData的方法
   
```
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
    public class TestController : Controller
    {
        private readonly IModelService _modelService;
        public TestController(IModelService modelService)
        {
            this._modelService = modelService;
        }  
        /// <summary>
        /// 測試TestData api
        /// </summary>
        [HttpGet]
        public  IActionResult GetTestData()
        {   
            try
            {
                 return new JsonResult(_modelService.GetTestData());
            }
            catch(Exception ex)
            {
                return new JsonResult(new BadRequestObjectResult(ex.Message));
            }
        }
    }
}
```
* * *
### Break down into end to end tests

Press F5 (Start Debugging) within Visual Studio Code 
* * *

## Deployment

1. cd D:\DLADJ\DLAdjApi (Project Folder Path)
dotnet publish --framework netcoreapp2.0 --output "D:\DLAdjWebAPI_Release" --configuration Release  --version-suffix 2.0.7


2. 將Build的檔案,搬檔到Web Server, AiAdj API 對應folder
 
    2.1. 開發環境(\DLADJ\DLAdjApi\bin\Release\netcoreapp2.0\publish") 
    將下列檔案搬到AiAdj API 對應folder
   ![Alt text](./image/publish.PNG "Optional title")  

    2.2. 設定appsettings.json
     AiAdj API 對應folder(C:\\DLAdj\API) 設定appsettings.json 
   ![Alt text](./image/config.PNG "Optional title")

    2.3. 使用Swagger測試Api
    http://10.91.1.108/api/index.html
  ![Alt text](./image/swagger.PNG "Optional title")

* * *

## Versioning

We use [SemVer](http://semver.org/) for versioning. For the versions available, see the [tags on this repository](https://github.com/your/project/tags). 
* * *
## Authors
Wise.shen
* * *