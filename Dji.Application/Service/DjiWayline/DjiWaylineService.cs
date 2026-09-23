using Dji.Application.Const;
using Dji.Application.Service.DjiWayline.Dto;
using Dji.Application.Service.DjiWayline.Dto.Temp;
using Dji.Core.Entity.DjiEntity;
using Dji.Core.Service;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Threading.Tasks;
namespace Dji.Application;
/// <summary>
/// 设备服务
/// </summary>
[ApiDescriptionSettings(ApplicationConst.GroupName, Order = 100)]
public class DjiWaylineService(SqlSugarRepository<DjiWaylineEntity> rep,DjiDeviceEnumService deviceEnumService,DjiDeviceCameraEnumService deviceCameraEnumService) : IDynamicApiController, ITransient
{
    private readonly SqlSugarRepository<DjiWaylineEntity> _rep = rep;
    private readonly DjiDeviceEnumService _deviceEnumService= deviceEnumService;
    private readonly DjiDeviceCameraEnumService _djiDeviceCameraEnumService= deviceCameraEnumService;

    public async Task<bool> CreateWayline(CreateWaypointWaylineRequest request)
    {
        var waylineNameValid=await _rep.IsAnyAsync(m=>m.WaylineName== request.WaylineName.Trim());
        if (waylineNameValid)
            throw Oops.Oh("航线名称已存在");
        WaylinesWpml waylinesWpml = new()
        { 
            Document= new WaylinesDocument() { 
             MissionConfig= CreateMisson(request), 
            }
        };
        return true;
    }

    private WaylineMissionConfig CreateMisson(CreateWaypointWaylineRequest request,bool isTmep=false)
    {
        var missionConfig = new WaylineMissionConfig
        {
            FlyToWaylineMode = request.MissionConfig.FlyToWaylineMode,
            FinishAction = request.MissionConfig.FinishAction,
            ExitOnRCLost = "executeLostAction",
            ExecuteRCLostAction = "goBack",
            TakeOffSecurityHeight = request.MissionConfig.TakeOffSecurityHeight,
            GlobalTransitionalSpeed = request.MissionConfig.GlobalTransitionalSpeed,
            GlobalRTHHeight = 100, 
        };
        var curDeviceEnum= _deviceEnumService.GetDeviceEnum(request.DomainTypeSubType) ?? throw Oops.Oh("未找到对应的设备信息"); 
        var droneInfo = new DroneInfo
        {
           DroneEnumValue = curDeviceEnum.Type,
           DroneSubEnumValue = curDeviceEnum.SubType ,
        };
        var curCameraEnum = _djiDeviceCameraEnumService.GetCameraEnum(curDeviceEnum.Id) ?? throw Oops.Oh("未找到对应的相机信息");
        var payloadInfo = new PayloadInfo
        {
            PayloadEnumValue = curCameraEnum.TsgIndex.Split("-").First().ToInt(),
            PayloadPositionIndex = curCameraEnum.TsgIndex.Split("-").Last().ToInt(),
        };
        missionConfig.DroneInfo = droneInfo;
        missionConfig.PayloadInfo = payloadInfo;
        return missionConfig;

    }

    private WaylineFolder CreateFolder(CreateWaypointWaylineRequest request)
    {
        List<Waypoint > points= [];
        for (int i = 0; i < request.Folder.Placemarks.Count; i++)
        {
                var item = request.Folder.Placemarks[i];
                Waypoint waypoint = new()
                {
                    Point = new PointCoordinates() { Coordinates=item.Point },
                    Index = i,
                    ExecuteHeight = item.ExecuteHeight,
                    WaypointSpeed = request.Folder.AutoFlightSpeed,
                };
                points.Add(waypoint);
        }
        var folder= new WaylineFolder
        {
            TemplateId=0,
            WaylineId=0,
            AutoFlightSpeed=request.Folder.AutoFlightSpeed,
            ExecuteHeightMode = "WGS84",
        };

        return folder;
    }

}

