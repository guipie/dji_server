// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023 yanyi  联系电话/微信：18600766045  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

global using Dji.Core.Service;
global using Dji;
global using Dji.ClayObject;
global using Dji.ConfigurableOptions;
global using Dji.DatabaseAccessor;
global using Dji.DataEncryption;
global using Dji.DataValidation;
global using Dji.DependencyInjection;
global using Dji.DynamicApiController;
global using Dji.EventBus;
global using Dji.FriendlyException;
global using Dji.JsonSerialization;
global using Dji.Logging;
global using Dji.RemoteRequest.Extensions;
global using Dji.Schedule;
global using Dji.UnifyResult;
global using Dji.ViewEngine;
global using Magicodes.ExporterAndImporter.Core;
global using Magicodes.ExporterAndImporter.Core.Extension;
global using Magicodes.ExporterAndImporter.Excel;
global using Mapster;
global using Microsoft.AspNetCore.Authorization;
global using Microsoft.AspNetCore.Http;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.AspNetCore.Mvc.Filters;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Options;
global using NewLife;
global using NewLife.Caching;
global using SKIT.FlurlHttpClient;
global using SKIT.FlurlHttpClient.Wechat.Api;
global using SKIT.FlurlHttpClient.Wechat.Api.Models;
global using SKIT.FlurlHttpClient.Wechat.TenpayV3;
global using SKIT.FlurlHttpClient.Wechat.TenpayV3.Events;
global using SKIT.FlurlHttpClient.Wechat.TenpayV3.Models;
global using SKIT.FlurlHttpClient.Wechat.TenpayV3.Settings;
global using SqlSugar;
global using System.Collections;
global using System.Collections.Concurrent;
global using System.ComponentModel;
global using System.ComponentModel.DataAnnotations;
global using System.Data;
global using System.Diagnostics;
global using System.Linq.Dynamic.Core;
global using System.Linq.Expressions;
global using System.Reflection;
global using System.Runtime.InteropServices;
global using System.Text;
global using System.Text.RegularExpressions;
global using System.Web;
global using UAParser;
global using Yitter.IdGenerator;
global using System;