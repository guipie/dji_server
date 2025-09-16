
namespace Admin.NET.Web.Entry.Controllers
{
    [Route("app/api/[controller]")]
    public class AppBaseController : ControllerBase
    {
        //[NonAction]
        //protected IActionResult Succes(string msg)
        //{
        //    return new JsonResult(new AppResult<object>() { Message = msg, Code = 200, Type = "success" });
        //}
        //[NonAction]
        //protected IActionResult Succes<T>(T data)
        //{
        //    return new JsonResult(new AppResult<object>() { Message = "操作成功", Code = 200, Type = "success", Result = data });
        //}


        //[NonAction]
        //protected IActionResult Warning(string msg)
        //{
        //    return new JsonResult(new AppResult<object>() { Message = msg, Code = 500, Type = "warning" });
        //}
        //[NonAction]
        //protected IActionResult Warning<T>(T data)
        //{
        //    return new JsonResult(new AppResult<object>() { Message = "操作出错", Code = 500, Type = "warning", Result = data });
        //}
    }
}