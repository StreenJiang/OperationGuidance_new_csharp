using System.Text.RegularExpressions;

namespace OperationGuidance_new.Utils
{
    public static class MissionNameHelper
    {
        // 只匹配本功能生成的 "{id} - " 格式（数字与短横线之间必须有空白），
        // 避免把外部配方码中自带的 "123-456" 误当成 ID 前缀。
        private static readonly Regex IdPrefixRegex = new(@"^\s*\d+\s+-\s+", RegexOptions.Compiled);

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

        /// <summary>
        /// 去掉任意 "{数字} - " 前缀，用于与来源未知（如配置、MES 下发）的名称比较。
        /// 与 <see cref="StripPrefix"/> 不同，这里不要求知道任务 id。
        /// </summary>
        public static string BaseName(string? name) =>
            string.IsNullOrEmpty(name) ? string.Empty : IdPrefixRegex.Replace(name, string.Empty, 1);

        /// <summary>
        /// 判断两个名称是否指同一个任务：先按全名精确比较，再忽略 id 前缀比较。
        /// </summary>
        public static bool NameEquals(string? a, string? b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return a == b;
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase)
                || string.Equals(BaseName(a), BaseName(b), StringComparison.OrdinalIgnoreCase);
        }
    }
}
