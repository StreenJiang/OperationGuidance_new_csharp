using CustomLibrary.Configs;
using CustomLibrary.Forms;
using CustomLibrary.Utils;
using OperationGuidance_new.Utils;
using OperationGuidance_new.Views.AbstractViews;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_new.Views.SubViews {
    public class SidePopUpForm: CustomPopUpForm2 {
        private AWorkplaceContentPanel _workplace;
        public Label ProductSideTitle { get; set; }
        public PictureBox SmallSideImage { get; set; }
        public PageSwitchButton First { get; set; }
        public PageSwitchButton Backward { get; set; }
        public Label PageInfo { get; set; }
        public PageSwitchButton Forward { get; set; }
        public PageSwitchButton Last { get; set; }
        public TableLayoutPanel ButtonPanel { get; set; }

        public SidePopUpForm(AWorkplaceContentPanel workplace) {
            _workplace = workplace;

            // Title of current side
            ProductSideTitle = new() {
                Parent = ContentPanel,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = ColorConfigs.COLOR_TEXT_BOX_FOREGROUND,
            };
            SmallSideImage = new() {
                Parent = ContentPanel,
                BackColor = ColorConfigs.COLOR_EMPTY_PRODUCT_CONTENT_BACKGROUND,
            };
            // Page info
            int currentPage = 1;
            int totalPages = 1;
            List<ProductSideDTO>? productSides = workplace._mission.ProductSides;
            if (productSides != null) {
                ProductSideTitle.Text = productSides[0].name;
                totalPages = productSides.Count;
            }
            First = new() {
                Icon = Properties.Resources.page_btn_backward_fast,
                TotalPages = totalPages,
                CurrentPage = currentPage,
            };
            Backward = new() {
                Icon = Properties.Resources.page_btn_backward,
                TotalPages = totalPages,
                CurrentPage = currentPage,
            };
            PageInfo = new() {
                Margin = new(0),
                Padding = new(0),
                AutoSize = true,
                ForeColor = ColorConfigs.COLOR_TEXT_BOX_FOREGROUND,
            };
            Forward = new() {
                Icon = Properties.Resources.page_btn_forward,
                TotalPages = totalPages,
                CurrentPage = currentPage,
            };
            Last = new() {
                Icon = Properties.Resources.page_btn_forward_fast,
                TotalPages = totalPages,
                CurrentPage = currentPage,
            };
            PageInfo.Text = Text = $"{currentPage}/{totalPages}";
            ButtonPanel = new() {
                Parent = ButtonsPanel,
                Margin = new(0),
                Padding = new(0),
                ColumnCount = 5,
            };
            ButtonPanel.Controls.Add(First);
            ButtonPanel.Controls.Add(Backward);
            ButtonPanel.Controls.Add(PageInfo);
            ButtonPanel.Controls.Add(Forward);
            ButtonPanel.Controls.Add(Last);

            First.Click += (sender, eventArgs) => {
                if (workplace._mission.id > 0) {
                    workplace._currentSideIndex = 0;
                    workplace.ChangeSideAndInvalidate();
                    PageInfo.Text = Text = $"{workplace._currentSideIndex + 1}/{totalPages}";
                }
            };
            Backward.Click += (sender, eventArgs) => {
                if (workplace._mission.id > 0) {
                    if (workplace._currentSideIndex <= 0) {
                        workplace._currentSideIndex = 0;
                    } else {
                        workplace._currentSideIndex -= 1;
                    }
                    workplace.ChangeSideAndInvalidate();
                    PageInfo.Text = Text = $"{workplace._currentSideIndex + 1}/{totalPages}";
                }
            };
            Forward.Click += (sender, eventArgs) => {
                if (workplace._mission.id > 0) {
                    if (workplace._currentSideIndex >= workplace._missionImages.Count - 1) {
                        workplace._currentSideIndex = workplace._missionImages.Count - 1;
                    } else {
                        workplace._currentSideIndex += 1;
                    }
                    workplace.ChangeSideAndInvalidate();
                    PageInfo.Text = Text = $"{workplace._currentSideIndex + 1}/{totalPages}";
                }
            };
            Last.Click += (sender, eventArgs) => {
                if (workplace._mission.id > 0) {
                    workplace._currentSideIndex = workplace._missionImages.Count - 1;
                    workplace.ChangeSideAndInvalidate();
                    PageInfo.Text = Text = $"{workplace._currentSideIndex + 1}/{totalPages}";
                }
            };
        }

        public void ResizeSelf() {
            int buttonsAreaWidth = ButtonsPanel.Width - ButtonsPanel.Padding.Size.Width;
            int buttonsAreaHeight = ButtonsPanel.Height - ButtonsPanel.Padding.Size.Height;
            int switchBtnSide = (int) (buttonsAreaHeight * .725);
            int vPadding = (buttonsAreaHeight - switchBtnSide) / 2;
            int hPadding = (int) (switchBtnSide * .5);

            PageInfo.Font = new(WidgetsConfigs.SystemFontFamily, switchBtnSide * .75F, FontStyle.Regular, GraphicsUnit.Pixel);
            First.Size = new(switchBtnSide, switchBtnSide);
            Backward.Size = new(switchBtnSide, switchBtnSide);
            Forward.Size = new(switchBtnSide, switchBtnSide);
            Last.Size = new(switchBtnSide, switchBtnSide);

            First.Margin = new(0, vPadding, hPadding, 0);
            Backward.Margin = new(0, vPadding, hPadding, 0);
            PageInfo.Margin = new(0, vPadding, hPadding, 0);
            Forward.Margin = new(0, vPadding, hPadding, 0);
            Last.Margin = new(0, vPadding, 0, 0);

            ButtonPanel.Size = new(switchBtnSide * 4 + PageInfo.Width + hPadding * 4 + ContentPanel.Padding.Right, buttonsAreaHeight);
            ButtonPanel.Location = new(ContentPanel.Padding.Left, 0);

            int contentVPadding = ContentPanel.Padding.Top;
            ProductSideTitle.Font = new(WidgetsConfigs.SystemFontFamily, WidgetUtils.PopUpOrFloatingFormTextOrComboBoxHeight() * .55F, FontStyle.Regular, GraphicsUnit.Pixel);
            int sideTitleHeight = ProductSideTitle.Font.Height;
            int contentRealWidth = ButtonsInnerPanel.Width + ButtonPanel.Width;
            int smallImageHeight = _workplace._productImageDisplayPanel.Height * contentRealWidth / _workplace._productImageDisplayPanel.Width;
            int contentRealHeight = contentVPadding + smallImageHeight + sideTitleHeight;

            ProductSideTitle.Size = new(contentRealWidth, sideTitleHeight);
            SmallSideImage.Size = new(contentRealWidth, smallImageHeight);
            SmallSideImage.Margin = new(0, contentVPadding, 0, 0);
            ResetImage();

            Size contentSize = new(ContentPanel.Padding.Size.Width + contentRealWidth, ContentPanel.Padding.Size.Height + contentRealHeight);
            SetContentSizeAndSelfSize(contentSize);
        }

        public void ResetImage() {
            if (_workplace._mission.id > 0) {
                Image? image = _workplace._missionImages[_workplace._currentSideIndex];
                if (image != null) {
                    float zoomingRatio = MainUtils.GetZoomingRatio(image.Size, SmallSideImage.Size);
                    Image imageTemp = MainUtils.ResizeImageByZoomingRatio(image, zoomingRatio);
                    SmallSideImage.Image = imageTemp;
                    if (SmallSideImage.Width > imageTemp.Width) {
                        int iHPadding = (SmallSideImage.Width - imageTemp.Width) / 2;
                        SmallSideImage.Padding = new(iHPadding, 0, iHPadding, 0);
                    } else if (SmallSideImage.Height > imageTemp.Height) {
                        int iVPadding = (SmallSideImage.Height - imageTemp.Height) / 2;
                        SmallSideImage.Padding = new(0, iVPadding, 0, iVPadding);
                    }
                }
            } else {
                float zoomingRatio = MainUtils.GetZoomingRatio(_workplace._defaultImage.Size, SmallSideImage.Size) * .5F;
                Image imageTemp = MainUtils.ResizeImageByZoomingRatio(_workplace._defaultImage, zoomingRatio);
                SmallSideImage.Image = imageTemp;
                int iHPadding = (SmallSideImage.Width - imageTemp.Width) / 2;
                int iVPadding = (SmallSideImage.Height - imageTemp.Height) / 2;
                SmallSideImage.Padding = new(iHPadding, iVPadding, iHPadding, iVPadding);
            }
        }
    }
}

