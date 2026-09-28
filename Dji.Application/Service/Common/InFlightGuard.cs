// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using System.Collections.Concurrent;

namespace Dji.Application.Service.Common;

/// <summary>
/// 「同资源正在下发中」标记（防连点 / 防并发重复下发）。
/// </summary>
/// <remarks>
/// <para>
/// <b>它解决的是真实存在的空窗</b>：设备指令的下发链路是「发 MQTT → 等 <c>services_reply</c> → 落库」，
/// 中间最长有十几秒数据库里<b>还没有这条记录</b>（协议的 <c>bid</c> 要发送时才生成，事前无法落库）。
/// 这段时间里「查数据库判断是否已有同类任务」永远返回 false，用户连点两次就会下发两条
/// <c>device_format</c> / <c>ota_create</c>。
/// </para>
/// <para>
/// <b>为什么用内存而不是数据库</b>：这只是「早一点、友好一点地拦住重复点击」，
/// 不承担跨实例的强一致职责（本平台为单实例部署）。
/// 真正的重复下发最终也会被设备侧拒绝，这里是尽力而为的一层保护。
/// </para>
/// <para>
/// <b>用法</b>：<c>using var _ = InFlightGuard.Enter(key, "机场重启");</c> ——
/// 已被占用时直接抛业务异常，作用域结束时自动释放；异常路径由 <c>using</c> 保证不会漏放。
/// </para>
/// </remarks>
public sealed class InFlightGuard : IDisposable
{
    /// <summary>全部占用中的标记（键由调用方按资源拼装，如「机场SN|指令」）</summary>
    private static readonly ConcurrentDictionary<string, byte> Marks = new();

    private readonly string _key;

    private InFlightGuard(string key) => _key = key;

    /// <summary>
    /// 占用标记。
    /// </summary>
    /// <param name="key">资源键，同键互斥</param>
    /// <param name="actionName">操作名（拼进异常文案，让用户知道是哪一步在被重复触发）</param>
    /// <returns>可释放的标记作用域</returns>
    /// <remarks>该资源已有下发在进行时直接抛业务异常（<c>Oops.Oh</c>）。</remarks>
    public static InFlightGuard Enter(string key, string actionName)
    {
        if (!Marks.TryAdd(key, 0))
            throw Oops.Oh($"【{actionName}】正在下发中，请勿重复操作");

        return new InFlightGuard(key);
    }

    /// <summary>释放标记</summary>
    public void Dispose() => Marks.TryRemove(_key, out _);
}
