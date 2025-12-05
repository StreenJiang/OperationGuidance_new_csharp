using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_new.Tasks.Abstracts;

namespace OperationGuidance_new.Tasks.Interfaces {
    /// <summary>
    /// 设备管理器接口
    /// 定义设备管理的通用操作：创建、更新、删除、重连
    /// </summary>
    /// <typeparam name="TDto">设备DTO类型</typeparam>
    /// <typeparam name="TTask">设备任务类型</typeparam>
    public interface IDeviceManager<TDto, TTask>
        where TDto : ADTOBase
        where TTask : ATaskBase {

        /// <summary>
        /// 创建设备（如果已存在则更新）
        /// </summary>
        /// <param name="dto">设备DTO</param>
        /// <param name="workstationId">工作站ID（可选）</param>
        /// <returns>创建或更新的设备任务实例</returns>
        TTask? CreateOrUpdateDevice(TDto dto, int? workstationId = null);

        /// <summary>
        /// 删除已从服务器端标记为删除的设备
        /// </summary>
        /// <param name="activeDtos">当前活跃的设备DTO列表</param>
        /// <returns>被删除的设备数量</returns>
        int RemoveDeletedDevices(IEnumerable<TDto> activeDtos);

        /// <summary>
        /// 检查设备是否需要重新连接
        /// </summary>
        /// <param name="task">设备任务实例</param>
        /// <param name="dto">对应的DTO</param>
        /// <returns>是否需要重连</returns>
        bool NeedsReconnection(TTask task, TDto dto);

        /// <summary>
        /// 执行设备重连
        /// </summary>
        /// <param name="task">设备任务实例</param>
        /// <param name="deviceInfo">设备信息（用于日志）</param>
        /// <returns>重连是否成功</returns>
        Task<bool> ReconnectAsync(TTask task, string deviceInfo);

        /// <summary>
        /// 获取设备信息描述
        /// </summary>
        /// <param name="task">设备任务实例</param>
        /// <returns>设备信息描述字符串</returns>
        string GetDeviceInfo(TTask task);

        /// <summary>
        /// 同步设备列表：创建/更新活跃设备，删除已删除设备
        /// </summary>
        /// <param name="dtos">设备DTO列表</param>
        /// <param name="workstationMap">工作站映射表（设备ID -> 工作站ID）</param>
        /// <returns>处理的设备数量</returns>
        Task<int> SynchronizeDevicesAsync(IEnumerable<TDto> dtos, Dictionary<int, int> workstationMap);
    }
}
