


namespace Admin.NET.Web.Entry;

public class AppResult<T> : AdminResult<T>
{
    public AppResult() { }
    public AppResult(string msg, T data, int code = 200)
    {
        this.Message = msg;
        this.Result = data;
        this.Code = code;
        this.Type = code == 200 ? "success" : "warning";
    }
}
