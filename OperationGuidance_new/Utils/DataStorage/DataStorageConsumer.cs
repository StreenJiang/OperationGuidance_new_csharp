using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using log4net;
using OperationGuidance_new.Configs;
using OperationGuidance_new.Configs.DTOs;
using OperationGuidance_new.Constants;
using OperationGuidance_new.ViewObjects;
using OperationGuidance_new.Views.AbstractViews;
using OperationGuidance_new.Views.SubViews;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Utils;
using CustomLibrary.Utils;

namespace OperationGuidance_new.Utils.DataStorage {
    public class DataStorageConsumer {
        private readonly ILog _logger;
        private readonly AWorkplaceContentPanel _panel;
        private readonly System.Threading.Channels.Channel<DataStorageMessage> _channel;
        private readonly bool _hideLooseningData; // 缓存，避免每条消息读 INI
        private Task _consumeTask;
        private volatile bool _consumerCrashed;

        public DataStorageConsumer(ILog logger, AWorkplaceContentPanel panel,
                System.Threading.Channels.Channel<DataStorageMessage> channel) {
            _logger = logger;
            _panel = panel;
            _channel = channel;
            // 缓存配置，避免每条拧紧消息都读 INI 文件 + 反射
            var settings = ConfigUtils.LoadConfig<Settings>();
            _hideLooseningData = settings.hide_loosening_data_in_workplace.ToYesOrNoBool();
        }

        public Task ConsumeTask => _consumeTask;
        public event Action? Crashed;

        public void Start(CancellationToken ct) {
            _consumeTask = Task.Run(() => ConsumeLoopAsync(ct), ct);
        }

        private async Task ConsumeLoopAsync(CancellationToken ct) {
            try {
                await foreach (var msg in _channel.Reader.ReadAllAsync(ct).ConfigureAwait(false)) {
                    switch (msg) {
                        case TighteningDataMessage t:
                            // Phase 1: DB 写入（可重试）
                            OperationDataDTO? storedDto = null;
                            bool dbOk = await ProcessWithRetry(async () => {
                                storedDto = await _panel.StoreDataToDatabaseAsync(t.Data).ConfigureAwait(false);
                            }, ct);
                            if (!dbOk) continue; // 用户终止

                            // Phase 2: UI 更新 + 钩子（DB 已成功，不重复 DB 写入）
                            // 幂等保护：Phase 2 成功部分不重复执行
                            var vo = ConvertToVO(t.Data);
                            bool voAdded = false, hookCalled = false;
                            if (!await ProcessWithRetry(async () => {
                                if (_hideLooseningData
                                        && t.Data.result_type != (int)TightenOrLoosen.TIGHTENING) {
                                    return;
                                }
                                if (!voAdded) {
                                    _panel.TighteningDataVOs.Add(vo);
                                    voAdded = true;
                                }
                                var snapshot = _panel.TighteningDataVOs.ToList();
                                SafeBeginInvoke(() => _panel.RefreshTighteningDataPanel(snapshot));
                                if (!hookCalled) {
                                    hookCalled = true; // 先标记，防止钩子抛异常时重试重复调用
                                    await _panel.OnTighteningDataStored(t.Data).ConfigureAwait(false);
                                }
                            }, ct)) continue;
                            break;
                        case CurveDataMessage c:
                            if (!await ProcessWithRetry(async () => {
                                var opId = _panel.currentOperationData?.id;
                                if (opId != null) {
                                    CurveDataDTO dto = new();
                                    CommonUtils.ObjectConverter<CurveDataTemp, CurveDataDTO>(c.Data, dto);
                                    dto.operation_data_id = opId.Value;
                                    _panel.Apis.AddOrUpdateCurveData(new(dto));
                                } else {
                                    _logger.Error("[Consumer] CurveData but currentOperationData is null");
                                }
                            }, ct)) continue;
                            break;
                        case ExportDataMessage e:
                            if (!await ProcessWithRetry(async () => {
                                var snapshot = _panel.TighteningDataVOs.ToList();
                                if (_panel.MissionRecord?.parts_bar_code != null) {
                                    foreach (var vo in snapshot)
                                        vo.parts_bar_code = _panel.MissionRecord.parts_bar_code;
                                }
                                var request = new ExportRequest {
                                    Data = snapshot,
                                    Fields = _panel.ExportFields,
                                    BasePath = _panel.ExportBasePath,
                                    ProductBatch = _panel.MissionRecord?.product_batch,
                                    ProductBarCode = _panel.MissionRecord?.product_bar_code,
                                    CompletedAt = DateTime.Now,
                                    Result = e.Result,
                                    EnableExcel = _panel.IsExcelExportEnabled,
                                    EnableTxt = _panel.IsTxtExportEnabled,
                                    MissionName = _panel.Mission?.name,
                                    WorkstationName = snapshot.Count > 0 ? snapshot[0].workstation_name : "",
                                };
                                await new DataExportService().ExportAsync(request).ConfigureAwait(false);
                                _panel.TighteningDataVOs.Clear();
                                SafeBeginInvoke(() => _panel.RefreshTighteningDataPanel(new List<OperationDataVO>()));
                            }, ct)) continue;
                            break;
                    }
                }
            } catch (OperationCanceledException) {
                _logger.Info("[Consumer] Cancelled");
            } catch (Exception ex) {
                _logger.Fatal("[Consumer] Crashed", ex);
                Crashed?.Invoke();
            }
        }

        private static readonly int[] RetryDelaysMs = { 1000, 2000, 4000 };
        private static readonly int RetryMaxAttempts = RetryDelaysMs.Length;
        private static readonly int RetryTotalWaitSeconds = RetryDelaysMs.Sum() / 1000;

        private async Task<bool> ProcessWithRetry(Func<Task> action, CancellationToken ct) {
            for (int attempt = 0; attempt <= RetryMaxAttempts; attempt++) {
                try {
                    await action().ConfigureAwait(false);
                    return true;
                } catch (OperationCanceledException) { throw; }
                catch (Exception ex) {
                    _logger.Warn($"[Consumer] Attempt {attempt + 1} failed: {ex.Message}");
                    if (attempt == RetryMaxAttempts) {
                        bool shouldRetry = await ShowRetryPopupAsync(RetryMaxAttempts, RetryTotalWaitSeconds);
                        if (!shouldRetry) return false;
                        // [重试] — 重跑 action（对于 Phase 2 调用，不包含 DB 写入）
                        try {
                            await action().ConfigureAwait(false);
                            return true;
                        } catch (OperationCanceledException) { throw; }
                        catch (Exception ex2) {
                            _logger.Error($"[Consumer] Final retry also failed: {ex2.Message}");
                            return false; // 最终放弃，跳过消息
                        }
                    }
                    await Task.Delay(RetryDelaysMs[attempt], ct).ConfigureAwait(false);
                }
            }
            return false;
        }

        private Task<bool> ShowRetryPopupAsync(int retryCount, int totalWaitSeconds) {
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _panel.BeginInvoke(new Action(() => {
                using var popup = new RetryPopupForm(retryCount, totalWaitSeconds);
                popup.Show();
                tcs.TrySetResult(popup.ShouldRetry);
                // [终止任务]: 不在 BeginInvoke 内排队 TerminateMission（避免死循环）
                // consumer 收到 false 后负责继续排空，TerminateMission 由外部触发
            }));
            return tcs.Task;
        }

        private void SafeBeginInvoke(Action action) {
            if (!_panel.IsDisposed && _panel.IsHandleCreated) {
                try { _panel.BeginInvoke(action); }
                catch (ObjectDisposedException) { }
                catch (InvalidOperationException) { }
            }
        }

        private static OperationDataVO ConvertToVO(OperationDataDTO dto) {
            OperationDataVO vo = new();
            CommonUtils.ObjectConverter<OperationDataDTO, OperationDataVO>(dto, vo);
            return vo;
        }
    }
}
