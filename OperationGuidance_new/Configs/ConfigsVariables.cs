namespace OperationGuidance_new.Configs {
    public static class ConfigsVariables {
        #region 路径相关配置
        #endregion

        #region 工作台配置
        // 条码输入框的默认文字
        public static string BAR_CODE_NOTE = "扫描录入或点击输入条码信息";
        #endregion
        
        #region 任务管理配置
        // 图片放大缩小的步值（每次放大缩小10%）
        public static float IMAGE_ZOOMING_RATIO_SETP => .1F;
        // 图片旋转的步值（每次旋转10°）
        public static float IMAGE_ROTATE_STEP => 10F;
        // 图片移动的步值（每次移动10%的身位）
        public static float IMAGE_MOVEMENT_STEP => .05F;
        #endregion

    }
}
