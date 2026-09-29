# 前端 dji-cloud-console 去 mock · 对接真实后端 dji_server

## Context（为什么做这件事）

前端 `d:\chenyanyi\dji\dji-cloud-console`（Tauri + Vue3 monorepo）当前默认走内置 mock 机队（`VITE_USE_MOCK=1` 或无 `VITE_HTTP_URL` 时 `DEFAULT_MOCK=true`），真实后端对接层虽已实现但被 mock 分支旁路。用户要求「根据已开发的后端完善前端、前端不要 mock 了」，并授权补全真实模式的 gap。

目标：**完全删除 mock 代码**，强制前端走真实 `dji_server`，并补全真实模式下设备列表初始化的缺口，使登录→设备拓扑→OSD→告警→DRC 全链路打通。

## 现状结论（探索已证实）

真实对接层已完整可用，无需重写：
- `packages/sdk/src/client/CloudRestClient.ts`：`getLoginConfig` / `getCaptcha` / `login`（SM2 加密 + 响应头 `access-token`/`x-access-token` 取 token）/ `refresh` / `logout` / `me` / `callWithRefresh`（401 续期重放一次）/ `fetchClientConfig`（后端无 bootstrap 接口已容错返回 null）。
- `packages/sdk/src/client/HttpClient.ts`：统一信封拆包（`result` 字段、`code===200`）、注入 `Authorization`+`X-Authorization`、401 触发 `onUnauthorized`。
- `packages/sdk/src/transport/SignalRTransport.ts`：拼接 `?access_token=<jwt>`、注册 `publicclientmessage`、按 `method` 二次分发。
- `packages/sdk/src/crypto/sm2.ts`：生成 `04||C1x||C1y||C3||C2` 与后端兼容密文。
- `apps/desktop/src/views/LoginView.vue`：真实模式 `loadLoginMeta()`→`getLoginConfig()`→`getCaptcha()`、`submit()`→`signIn({account,password,codeId,code})` 已通。

真实模式 gap（需补）：
1. `fleet.ts` 真实模式 `hydrateDevices()` 直接 return → 设备列表空。
2. `CloudApiClient.dispatch` 的 `topo` 分支只注释、未把设备合并进 `this.devices`、未推 `deviceListeners`。
3. `CloudRestClient` 无「拉设备列表」方法。

后端可用接口：
- `GET /api/djiDevice/list`（`DjiDeviceService.List`，返回 `List<DjiDeviceOutput>`）。
- SignalR 推送 `update_topo`（`MqDeviceService.DeviceManageAsync`，订阅 `ThingProductStatus`）。

## 实施步骤

### A. 删除 mock 实现代码
- 删除目录 `packages/sdk/src/mock/`（`simulator.ts`、`fleet.ts`、`index.ts`）。
- 删除 `packages/sdk/src/transport/MockTransport.ts`。

### B. SDK 出口清理
`packages/sdk/src/index.ts`：
- 移除第 9 行 `buildMockFleet` 导出（保留 `CloudApiClient`、`CloudApiClientOptions`）。
- 移除第 26 行 `export { MockTransport }`。
- 移除第 31 行 `export { createFleet, FleetSimulator }` 整行。

### C. CloudApiClient 去 mock 分支 + 实现 topo 合并
`packages/sdk/src/client/CloudApiClient.ts`：
- 删 import `MockTransport`、`createFleet`（第 11-12 行）。
- `CloudApiClientOptions`：移除 `useMock`、`dockCount`。
- 构造函数：移除 mock 分支，`signalRUrl` 缺失时抛明确错误（不再静默回退 mock）；保留 `WebSocketTransport` 分支给未来自建网关。
- **补 topo 合并**：`dispatch` 的 `case 'topo'` 把 `m.data` 里的设备合并进 `this.devices`，并通知 `deviceListeners`（这是真实模式设备增量的入口）。
- 删除文件末尾 `buildMockFleet()` 函数（第 157-176 行）。
- 更新 `getFleetSnapshot()` 注释。

### D. CloudRestClient 补「拉设备列表」
`packages/sdk/src/client/CloudRestClient.ts`：新增方法
```ts
getDeviceList(): Promise<Device[]>  // GET /api/djiDevice/list
```
路径常量放 `AUTH_PATHS` 旁新开一个 `DEVICE_PATHS = { list: '/api/djiDevice/list' }`。返回值做 `DjiDeviceOutput → Device` 字段映射（实现时读 `Dji.Core/Entity/DjiEntity/DjiDevice.cs` 与 `DjiDeviceOutput` DTO 确认字段：`Sn`、`ParentSn`、`DeviceType`/`SubType`、`Nick`、online 状态、经纬度等；dock 的 `gatewaySn=自身 Sn`，drone 的 `gatewaySn=ParentSn`）。

### E. 配置层改真实导向
`apps/desktop/src/stores/settings.ts`：
- `ConnectionSettings`：移除 `useMock`、`mockDocks`。
- 移除 `DEFAULT_MOCK`，默认 `httpUrl` 取 `VITE_HTTP_URL`，`wsUrl` 移除（SignalR 与 HTTP 同主机同端口，无需单独 WS 地址）。
- `defaults().connection` 同步精简。

`apps/desktop/.env.example`：
- 删 `VITE_USE_MOCK`、`VITE_MOCK_DOCKS`、`VITE_WS_URL`。
- `VITE_HTTP_URL=http://localhost:5005`（后端默认端口 5005，README 已述）。
- `VITE_SM2_PUBLIC_KEY=` 保留并强调必填（与后端 `App.json → Cryptogram.PublicKey` 成对）。

### F. cloud 单例去 mock
`apps/desktop/src/composables/cloud.ts`：
- `getRestClient()`：移除 `disablePasswordEncryption: settings.connection.useMock`（真实模式永远 SM2）。
- `getCloudClient()`：`signalRUrl` 直接用 `settings.connection.httpUrl`，移除 `useMock`/`dockCount`。

### G. fleet store 补真实初始化
`apps/desktop/src/stores/fleet.ts`：
- 移除 `buildMockFleet` import 与本地生成分支。
- `hydrateDevices()` 改为真实模式：调 `getRestClient().getDeviceList()` 拉全量 → 填充 `devices` Map → 默认选中首个 dock。
- `bindStreams()` 增加 `client.onDevices(...)` 监听 `update_topo` 增量（依赖 C 步实现的 topo 合并推送）。
- `init()` 顺序：`hydrateDevices()`（HTTP 拉全量）→ `bindStreams()` → `connect()`（SignalR 增量）。

### H. 登录页 / 设置页清理 mock 残留
`apps/desktop/src/views/LoginView.vue`：
- 移除 `form.useMock`、`demoHint`、`fillDemo()`、`onToggleMock()`、高级区 useMock checkbox 与 `v-if="!form.useMock"`、`demoHint` 提示块 + quickLogin、`loadLoginMeta` 的 mock 跳过分支。
- 保留真实模式 `loadLoginMeta()`/`submit()`/验证码逻辑。
- 高级区保留 httpUrl 输入（wsUrl 输入删除）。

`apps/desktop/src/views/SettingsView.vue`：删除「使用模拟数据」开关与模拟机场数量输入（约第 475-486 行）。

### I. i18n 清理（如有）
检查 `apps/desktop/src/i18n/locales/{zh-CN,zh-TW,en-US}.ts` 是否有 `login.useMock`/`login.demoHint`/`login.quickLogin`/`settings.connection.mock*` 等键，有则删除。

## 验证

1. **类型/构建**：在 `d:\chenyanyi\dji\dji-cloud-console` 跑 `pnpm install && pnpm -r typecheck`，确认无 mock 引用残留、无类型错误。
2. **后端就绪**：`dji_server` 已配置真实 `App.json`(SM2)、`JWT.json`、`Database.json`，`dotnet run --project Dji.Web.Entry` 监听 `http://0.0.0.0:5005`；种子账号 `superadmin`/`admin` 密码 `123456`。
3. **前端配置**：`apps/desktop/.env` 填 `VITE_HTTP_URL=http://localhost:5005` 与 `VITE_SM2_PUBLIC_KEY`（=后端 `Cryptogram.PublicKey`）。
4. **端到端（computer-use 辅助）**：实现用文件编辑工具完成；验证阶段可用 computer-use 插件启动 `pnpm --filter @dji/desktop dev`（或 Tauri dev），在应用内走：登录页→输 `superadmin`/`123456`→（若后端开验证码则填）→进入主页→设备列表来自 `/api/djiDevice/list`→OSD/HMS 由 SignalR `publicclientmessage` 推送→token 20 分钟过期后自动 `refreshToken` 续期。
5. **回归点**：登录失败换验证码、401 续期重放、登出清会话、切语言/主题不受影响。

## 范围外（本次不动）
- 后端无 `/api/config/bootstrap`，前端 `fetchClientConfig` 已容错返回 null，地图/直播配置继续走「环境变量兜底」，不补后端接口。
- DRC 杆量、直播、航线下发等业务指令通道（HTTP）若已接则不重写，只确认未被 mock 旁路。
