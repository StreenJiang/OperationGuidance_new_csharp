namespace OperationGuidance_new.Utils
{
    public static class MissionNameHelper
    {
        private static string MakePrefix(int id) => $"{id} - ";

        /// <summary>加载显示时去掉前缀。id ≤ 0 或名称为空时原样返回。</summary>
        public static string StripPrefix(string name, int id)
        {
            if (id <= 0 || string.IsNullOrEmpty(name)) return name;
            string prefix = MakePrefix(id);
            return name.StartsWith(prefix) ? name.Substring(prefix.Length) : name;
        }

        /// <summary>保存时加上前缀。id ≤ 0、名称为空、或已有前缀时原样返回（幂等）。</summary>
        public static string ApplyPrefix(string name, int id)
        {
            if (id <= 0 || string.IsNullOrEmpty(name)) return name;
            string prefix = MakePrefix(id);
            return name.StartsWith(prefix) ? name : prefix + name;
        }
    }
}
