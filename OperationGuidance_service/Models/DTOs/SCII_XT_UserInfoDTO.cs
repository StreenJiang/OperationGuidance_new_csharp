namespace OperationGuidance_service.Models.DTOs {
    public class SCII_XT_UserInfoDTO {
        public string message { get; set; } = string.Empty;

        public int id { get; set; }
        public string employeeNumber { get; set; } = string.Empty;
        public string employeeName { get; set; } = string.Empty;
        public string roleName { get; set; } = string.Empty;
        public int workshopId { get; set; }
        public string workshopName { get; set; } = string.Empty;
        public string position { get; set; } = string.Empty;
        public string phone { get; set; } = string.Empty;
        public int status { get; set; }
        public string creatorName { get; set; } = string.Empty;
        public string createTime { get; set; } = string.Empty;
        public string updaterName { get; set; } = string.Empty;
        public string updateTime { get; set; } = string.Empty;
    }
}
