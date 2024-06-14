using System.Collections.ObjectModel;
using CustomLibrary.Configs;
using CustomLibrary.Forms;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using LiveChartsCore.SkiaSharpView.WinForms;
using OperationGuidance_service.Models.DTOs;
using SkiaSharp;

namespace OperationGuidance_new.Views.ReusableWidgets {
    public class CurvePopUpForm: CustomPopUpForm {
        private CartesianChart _chart;
        private CurveDataDTO? _angleCurveData;
        private CurveDataDTO? _torqueCurveData;
        private List<string> _angleData;
        private List<double> _torqueData;

        public CartesianChart Chart { get => _chart; set => _chart = value; }
        public CurveDataDTO? AngleCurveData { get => _angleCurveData; set => _angleCurveData = value; }
        public CurveDataDTO? TorqueCurveData { get => _torqueCurveData; set => _torqueCurveData = value; }

        public CurvePopUpForm(int boltSerialNum, CurveDataDTO? angleCurveData, CurveDataDTO? torqueCurveData) {
            Title = $"曲线数据 - 点位{boltSerialNum}";
            BorderColor = ColorConfigs.COLOR_POP_UP_BORDER;

            _chart = new() {
                Parent = ContentPanel,
                ZoomMode = LiveChartsCore.Measure.ZoomAndPanMode.X,
            };

            AddButton("关闭").Click += (s, e) => Dispose();

            _angleCurveData = angleCurveData;
            _torqueCurveData = torqueCurveData;

            if (_angleCurveData != null) {
                List<string> dataStrs = _angleCurveData.data_samples.Split(",").ToList();
                _angleData = dataStrs.Select(str => str.Trim()).ToList();
                _chart.XAxes = new Axis[] {
                    new Axis {
                        Name = "ANGLE (°)",
                        NameTextSize = 15,
                        NamePaint = new SolidColorPaint(SKColors.Green),
                        LabelsPaint = new SolidColorPaint(SKColors.Green),
                        TextSize = 15,
                        Labels = _angleData,
                    },
                };
            } else {
                _angleData = new();
            }
            if (_torqueCurveData != null) {
                List<string> dataStrs2 = _torqueCurveData.data_samples.Split(",").ToList();
                _torqueData = dataStrs2.Select(str => {
                    str = str.Trim();
                    return double.Parse(str);
                }).ToList();
            } else {
                _torqueData = new();
            }

            _chart.Series = new ObservableCollection<ISeries> {
                new LineSeries<double>() {
                    Name = "TORQUE",
                    GeometrySize = 0,
                    LineSmoothness = 1,
                    Fill = null,
                    Stroke = new SolidColorPaint(SKColors.Red) { StrokeThickness = 1 },
                    Values = _torqueData,
                },
            };
            if (_angleCurveData != null) {
                List<string> dataStrs = _angleCurveData.data_samples.Split(",").ToList();
                dataStrs = dataStrs.Select(s => s.Trim()).ToList();
            };
            _chart.YAxes = new Axis[] {
                new Axis {
                    Name = "TORQUE (N*m)",
                    NameTextSize = 15,
                    NamePaint = new SolidColorPaint(SKColors.Green),
                    LabelsPaint = new SolidColorPaint(SKColors.Green),
                    TextSize = 15,
                    SeparatorsPaint = new SolidColorPaint(SKColors.LightSlateGray) {
                        StrokeThickness = 1,
                        PathEffect = new DashEffect(new float[] { 15, 15 })
                    }
                },
            };
        }

    }
}
