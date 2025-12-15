using CustomLibrary.Utils;
using log4net;
using OperationGuidance_new.Configs;
using OperationGuidance_new.Utils;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Utils;

namespace OperationGuidance_new.Views.ReusableWidgets {
    public class ProductImageFile: IDisposable {
        private ILog logger = LogManager.GetLogger(typeof(ProductImageFile));

        private AProductImageDisplayPanel _container;
        private ProductSideDTO _sideDTO;
        private string? _filePath;
        private Image? _image;
        private string? _imageFileName;
        private Rectangle _containerMaxRect;
        private Point _centerLocation;
        private Point _locationOffset;
        private Point _locationOffsetMoving;
        private float _zoomingRatio;
        private float _zoomingRatioExtra;
        private float _rotateAngle;
        private bool _cropped;
        private Stack<ProductImageFile> _undoBuffer;
        private int _undoBufferLength;
        private Rectangle? _imageRange;
        public AProductImageDisplayPanel Container { get => _container; set => _container = value; }
        public ProductSideDTO SideDTO { get => _sideDTO; set => _sideDTO = value; }
        public string? FilePath { get => _filePath; set => _filePath = value; }
        public Image? Image {
            get => _image;
            set {
                _image = value;
                if (value == null) {
                    _imageRange = null;
                }
            }
        }
        public string? ImageFileName { get => _imageFileName; set => _imageFileName = value; }
        public Rectangle ContainerMaxRect { get => _containerMaxRect; set => _containerMaxRect = value; }
        public Point CenterLocation { get => _centerLocation; set => _centerLocation = value; }
        public Point LocationOffset { get => _locationOffset; set => _locationOffset = value; }
        public Point LocationOffsetMoving { get => _locationOffsetMoving; set => _locationOffsetMoving = value; }
        public float ZoomingRatio { get => _zoomingRatio; set => _zoomingRatio = value; }
        public float ZoomingRatioExtra { get => _zoomingRatioExtra; set => _zoomingRatioExtra = value; }
        public float RotateAngle { get => _rotateAngle; set => _rotateAngle = value; }
        public bool Cropped { get => _cropped; set => _cropped = value; }
        public Rectangle? ImageRange { get => _imageRange; }

        public ProductImageFile(AProductImageDisplayPanel container, ProductSideDTO sideDTO, int undoBufferLength) {
            _container = container;
            _sideDTO = sideDTO;
            _undoBuffer = new();
            _undoBufferLength = undoBufferLength;

            Image? image = MainUtils.GetProductImage(sideDTO.image);
            if (image != null) {
                _imageFileName = sideDTO.image;
                _image = image;
                if (sideDTO.max_rectangle_width != null && sideDTO.max_rectangle_height != null) {
                    _containerMaxRect = new(CommonUtils.PointStringToPoint(sideDTO.max_rectangle_location), new(sideDTO.max_rectangle_width.Value, sideDTO.max_rectangle_height.Value));
                    _centerLocation = CommonUtils.PointStringToPoint(sideDTO.center_location);
                    _locationOffset = CommonUtils.PointStringToPoint(sideDTO.location_offset);
                    _locationOffsetMoving = CommonUtils.PointStringToPoint(sideDTO.location_offset_moving);
                    _zoomingRatio = sideDTO.zooming_ratio != null ? sideDTO.zooming_ratio.Value : 0;
                    _zoomingRatioExtra = sideDTO.zooming_ratio_extra != null ? sideDTO.zooming_ratio_extra.Value : 0;
                    _rotateAngle = sideDTO.rotate_angle != null ? sideDTO.rotate_angle.Value : 0;
                    _cropped = _sideDTO.cropped != null && _sideDTO.cropped == (int) YesOrNo.YES;
                }
            }
        }

        public ProductImageFile Copy() {
            // 添加安全检查
            Image? newImage = null;
            if (_image != null) {
                try {
                    // 检查图像是否仍然有效
                    if (_image.Width > 0 && _image.Height > 0) {
                        newImage = new Bitmap(_image);
                    } else {
                        logger.Warn($"警告: 图像尺寸无效 - Width: {_image.Width}, Height: {_image.Height}");
                    }
                } catch (ArgumentException ex) {
                    logger.Warn($"复制图像失败: {ex.Message}");
                    // 尝试重新加载
                    newImage = ReloadImage();
                } catch (ObjectDisposedException ex) {
                    logger.Warn($"图像已被释放: {ex.Message}");
                    // 尝试重新加载
                    newImage = ReloadImage();
                }
            }

            return new(_container, _sideDTO, _undoBufferLength) {
                FilePath = _filePath,
                ImageFileName = _imageFileName,
                Image = newImage,
                CenterLocation = _centerLocation,
                LocationOffset = _locationOffset,
                LocationOffsetMoving = _locationOffsetMoving,
                ZoomingRatio = _zoomingRatio,
                ZoomingRatioExtra = _zoomingRatioExtra,
                RotateAngle = _rotateAngle,
                Cropped = _cropped,
            };
        }
        private Image? ReloadImage() {
            Image? reloadedImage = null;

            // 首先尝试从 _filePath 加载图像（如果已设置）
            if (!string.IsNullOrEmpty(_filePath)) {
                try {
                    if (File.Exists(_filePath)) {
                        logger.Info($"正在从文件路径重新加载图像: {_filePath}");
                        reloadedImage = Image.FromFile(_filePath);
                        logger.Info($"成功从文件路径加载图像 - 尺寸: {reloadedImage.Width}x{reloadedImage.Height}");
                    } else {
                        logger.Warn($"文件路径不存在: {_filePath}");
                    }
                } catch (ArgumentException ex) {
                    logger.Warn($"从文件路径加载图像失败 (参数无效): {_filePath}, 错误: {ex.Message}");
                } catch (FileNotFoundException ex) {
                    logger.Warn($"文件未找到: {_filePath}, 错误: {ex.Message}");
                } catch (DirectoryNotFoundException ex) {
                    logger.Warn($"目录未找到: {_filePath}, 错误: {ex.Message}");
                } catch (UnauthorizedAccessException ex) {
                    logger.Warn($"无权限访问文件: {_filePath}, 错误: {ex.Message}");
                } catch (IOException ex) {
                    logger.Warn($"I/O错误，从文件路径加载图像失败: {_filePath}, 错误: {ex.Message}");
                } catch (Exception ex) {
                    logger.Error($"从文件路径加载图像时发生未知错误: {_filePath}", ex);
                }
            }

            // 如果从 _filePath 加载失败或未设置，则尝试使用 _imageFileName
            if (reloadedImage == null && !string.IsNullOrEmpty(_imageFileName)) {
                try {
                    logger.Info($"正在通过图像文件名重新加载图像: {_imageFileName}");
                    reloadedImage = MainUtils.GetProductImage(_imageFileName);
                    if (reloadedImage != null) {
                        logger.Info($"成功通过图像文件名加载图像 - 尺寸: {reloadedImage.Width}x{reloadedImage.Height}");
                    } else {
                        logger.Warn($"通过图像文件名未能加载图像: {_imageFileName}");
                    }
                } catch (ArgumentException ex) {
                    logger.Warn($"通过图像文件名加载图像失败 (参数无效): {_imageFileName}, 错误: {ex.Message}");
                } catch (FileNotFoundException ex) {
                    logger.Warn($"图像文件未找到: {_imageFileName}, 错误: {ex.Message}");
                } catch (Exception ex) {
                    logger.Error($"通过图像文件名加载图像时发生未知错误: {_imageFileName}", ex);
                }
            }

            if (reloadedImage == null) {
                logger.Warn("重新加载图像失败：既无法从文件路径加载，也无法通过图像文件名加载");
            }

            return reloadedImage;
        }

        /// <summary>
        /// 检查图像是否有效
        /// </summary>
        private bool IsImageValid(Image? image) {
            if (image == null) {
                return false;
            }

            try {
                _ = image.Width;
                _ = image.Height;
                return true;
            } catch (ArgumentException) {
                return false;
            } catch (ObjectDisposedException) {
                return false;
            }
        }

        public void CopyFrom(ProductImageFile from) {
            _filePath = from.FilePath;
            _imageFileName = from.ImageFileName;

            if (from.Image != null && IsImageValid(from.Image)) {
                try {
                    _image = new Bitmap(from.Image);
                } catch (ArgumentException ex) {
                    logger.Warn($"复制图像失败: {ex.Message}");
                    _image = ReloadImage();
                } catch (ObjectDisposedException ex) {
                    logger.Warn($"图像已被释放: {ex.Message}");
                    _image = ReloadImage();
                }
            } else {
                // 源图像无效或为 null，尝试重新加载
                logger.Info("源图像无效或为 null，正在尝试重新加载...");
                _image = ReloadImage();
            }

            _centerLocation = from.CenterLocation;
            _locationOffset = from.LocationOffset;
            _locationOffsetMoving = from.LocationOffsetMoving;
            _zoomingRatio = from.ZoomingRatio;
            _zoomingRatioExtra = from.ZoomingRatioExtra;
            _rotateAngle = from.RotateAngle;
            _cropped = from.Cropped;
        }

        public void ImageSelect(Action? afterSelected = null) {
            OpenFileDialog dialog = new() {
                Title = "请选择产品图片",
                Filter = "Picture file|*.jpg;*.jpeg;*.png",
            };
            if (dialog.ShowDialog() == DialogResult.OK) {
                ClearBuffer();
                _filePath = dialog.FileName;
                _image = Image.FromFile(_filePath);
                _centerLocation = new(0, 0);
                _locationOffset = new(0, 0);
                _locationOffsetMoving = new(0, 0);
                _zoomingRatio = 0;
                _zoomingRatioExtra = 0;
                _rotateAngle = 0;
                _cropped = false;
                RecalculateZoomingRatio();
                RefreshImage();

                if (afterSelected != null) {
                    afterSelected();
                }
            }
        }

        public void RecalculateZoomingRatio() {
            if (_image != null && (_containerMaxRect.Size != _container.MaxRectSize || _zoomingRatio == 0)) {
                _containerMaxRect = _container.MaxRect;
                _zoomingRatio = MainUtils.GetZoomingRatio(_image.Size, _container.MaxRectSize);
            }
        }

        public void ImageZoomIn() {
            _zoomingRatioExtra += ConfigsVariables.IMAGE_ZOOMING_RATIO_SETP;
            RefreshImage();
        }

        public void ImageZoomOut() {
            _zoomingRatioExtra -= ConfigsVariables.IMAGE_ZOOMING_RATIO_SETP;
            RefreshImage();
        }

        public void ImageRotateClockwise() {
            _rotateAngle += ConfigsVariables.IMAGE_ROTATE_STEP;
            if (_rotateAngle > 360) {
                _rotateAngle -= 360;
            }
            RefreshImage();
        }

        public void ImageRotateAntiClockwise() {
            _rotateAngle -= ConfigsVariables.IMAGE_ROTATE_STEP;
            if (_rotateAngle < -360) {
                _rotateAngle += 360;
            }
            RefreshImage();
        }

        public void ImageMoveUp() {
            Point locationOffset = new(0, -(int) (_containerMaxRect.Height * ConfigsVariables.IMAGE_MOVEMENT_STEP));
            _locationOffset.Offset(locationOffset);
            RefreshImage();
        }
        public void ImageMoveDown() {
            Point locationOffset = new(0, (int) (_containerMaxRect.Height * ConfigsVariables.IMAGE_MOVEMENT_STEP));
            _locationOffset.Offset(locationOffset);
            RefreshImage();
        }
        public void ImageMoveLeft() {
            Point locationOffset = new(-(int) (_containerMaxRect.Height * ConfigsVariables.IMAGE_MOVEMENT_STEP), 0);
            _locationOffset.Offset(locationOffset);
            RefreshImage();
        }
        public void ImageMoveRight() {
            Point locationOffset = new((int) (_containerMaxRect.Height * ConfigsVariables.IMAGE_MOVEMENT_STEP), 0);
            _locationOffset.Offset(locationOffset);
            RefreshImage();
        }

        public void ImageCrop() {
            // Get real time displayed image, and will have its size and location
            Image? imageDisplay = GetDisplayImage();
            Console.WriteLine($"imageDisplay: {imageDisplay}");
            if (imageDisplay != null) {
                // Set cropped to true here because if the image is smaller than rectangle, it won't go in the if block blow
                _cropped = true;

                // Check whether the image exceeds the cropping range
                Point croppingRectLocation = new(_container.MaxRectLocation.X - _centerLocation.X, _container.MaxRectLocation.Y - _centerLocation.Y);
                Point lowerRightConer = new(_centerLocation.X + imageDisplay.Width, _centerLocation.Y + imageDisplay.Height);
                if (!_container.MaxRect.Contains(_centerLocation) || !_container.MaxRect.Contains(lowerRightConer)) {
                    _image = MainUtils.CropImage(imageDisplay, new(croppingRectLocation, _container.MaxRectSize));
                    _locationOffset = new(0, 0);
                    _locationOffsetMoving = new(0, 0);
                    _zoomingRatio = 1;
                    _zoomingRatioExtra = 0;
                    _rotateAngle = 0;
                    RefreshImage();
                }
            }
        }

        public void ImageUndo() {
            if (_undoBuffer.Count > 0) {
                CopyFrom(_undoBuffer.Pop());
                RefreshImage();
            }
        }

        public void SaveCurrent() {
            if (_undoBuffer.Count < _undoBufferLength) {
                _undoBuffer.Push(this.Copy());
            }
        }

        public void ClearBuffer() {
            _undoBuffer.Clear();
        }

        public Image? GetDisplayImage() {
            // 尝试生成有效图像
            var result = TryCreateDisplayImage();
            if (result != null)
                return result;

            // 如果失败，尝试重载一次（避免无限重试）
            logger.Info("图像处理失败，尝试重新加载源图像...");
            _image = ReloadImage();

            // 再试一次
            return TryCreateDisplayImage();
        }

        private Image? TryCreateDisplayImage() {
            if (_image == null || !IsImageValid(_image))
                return null;

            try {
                float finalRatio = _zoomingRatio * (1 + _zoomingRatioExtra);
                using var originalCopy = (Image)_image.Clone(); // 安全起见，操作副本
                var resized = MainUtils.ResizeImageByZoomingRatio(originalCopy, finalRatio);

                if (_rotateAngle == 0)
                    return resized;

                // 注意：RotateImage 应返回新图像，且不 dispose 输入
                return WidgetUtils.RotateImage(resized, _rotateAngle, logger);
            } catch (ArgumentException ex) {
                logger.Warn($"图像参数无效: {ex.Message}");
            } catch (ObjectDisposedException ex) {
                logger.Warn($"图像已被释放: {ex.Message}");
            } catch (Exception ex) {
                logger.Error("处理图像时发生未预期错误", ex);
            }

            return null;
        }

        public void RefreshImage() {
            try {
                Image? imageDisplay = GetDisplayImage();
                if (imageDisplay != null && IsImageValid(imageDisplay)) {
                    _centerLocation = new((_container.Width - imageDisplay.Width) / 2, (_container.Height - imageDisplay.Height) / 2);
                    _centerLocation.X += _locationOffset.X;
                    _centerLocation.Y += _locationOffset.Y;
                    _centerLocation.X += _locationOffsetMoving.X;
                    _centerLocation.Y += _locationOffsetMoving.Y;
                    _container.SetImage(imageDisplay, _centerLocation);
                    _imageRange = new(_centerLocation, imageDisplay.Size);
                } else {
                    _container.SetImage(null, null);
                }
            } catch (ArgumentException ex) {
                logger.Warn($"刷新图像失败: {ex.Message}");
                _container.SetImage(null, null);
            } catch (ObjectDisposedException ex) {
                logger.Warn($"图像已被释放: {ex.Message}");
                _container.SetImage(null, null);
            } catch (Exception ex) {
                logger.Error("Error while refreshing image...", ex);
                _container.SetImage(null, null);
            }
            // WARN: this will cause extra time wastes, so can move to save button
            // SaveSideInfo();
        }

        public void SaveSideInfo() {
            if (_image != null && _imageFileName == null) {
                _imageFileName = MainUtils.GenerateProductImageName();
            }
            _sideDTO.image = _imageFileName;
            _sideDTO.zooming_ratio = _zoomingRatio;
            _sideDTO.zooming_ratio_extra = _zoomingRatioExtra;
            _sideDTO.center_location = _centerLocation.ToString();
            _sideDTO.location_offset = _locationOffset.ToString();
            _sideDTO.location_offset_moving = _locationOffsetMoving.ToString();
            _sideDTO.rotate_angle = _rotateAngle;
            _sideDTO.cropped = _cropped ? (int) YesOrNo.YES : (int) YesOrNo.NO;
        }

        public void Dispose() {
            _image?.Dispose();
        }
    }
}
