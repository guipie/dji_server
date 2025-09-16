
namespace Admin.NET.Core.Extension;

[SuppressSniffer]
public static partial class StringExtension
{
    readonly static string source = "123456789ABCDEFGHIJKLMNPQRSTUVWXYZabcdefghijklmnpqrstuvwxyz";
    readonly static string[] replaceSource = new string[2] { "9", "8" };
    public static string StrRandom(this string val)
    {
        if (val.IsNullOrEmpty() || val.Length > source.Length)
            return DateTime.Now.Millisecond.ToString();
        var result = string.Empty;
        var s = val.Split();
        for (int i = 0; i < s.Length; i++)
        {
            if (replaceSource.Contains(s[i]))
                result += "_";
            else
                result += s[i];
        }
        return result;
    }
    /// <summary>
    /// 随机生成不重复邀请码
    /// </summary>
    /// <param name="length">长度</param>
    /// <param name="seed">种子</param>
    /// <returns></returns>
    public static string CreateRandStrCode(int length, int seed = 0)
    {
        //Guid的哈希码作为种子值
        byte[] buffer = Guid.NewGuid().ToByteArray();
        var ranInt = BitConverter.ToInt32(buffer, 0) + seed;
        Random random = new(ranInt);
        string re = "";
        for (int i = 0; i < length; i++)
        {
            int number = random.Next(source.Length);
            re += source.Substring(number, 1);
        }
        return re;
    }

    public static int CreateRandNumCode(int length, int seed = 0)
    {
        //Guid的哈希码作为种子值
        byte[] buffer = Guid.NewGuid().ToByteArray();
        var ranInt = BitConverter.ToInt32(buffer, 0) + seed;
        Random random = new(ranInt);
        return random.Next(int.Parse("1".PadRight(length - 1, '0')), int.Parse("9".PadLeft(length - 1, '9')));
    }
}
