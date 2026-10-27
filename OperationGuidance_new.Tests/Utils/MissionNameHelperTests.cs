using OperationGuidance_new.Utils;
using Xunit;

namespace OperationGuidance_new.Tests.Utils {
    public class MissionNameHelperTests {
        [Theory]
        [InlineData("5 - 任务A", 5, "任务A")]
        [InlineData("5 - 任务A", 6, "5 - 任务A")]
        [InlineData("任务A", 5, "任务A")]
        [InlineData("", 5, "")]
        [InlineData("5 - 任务A", -1, "5 - 任务A")]
        public void StripPrefix_OnlyStripsOwnIdPrefix(string name, int id, string expected) {
            Assert.Equal(expected, MissionNameHelper.StripPrefix(name, id));
        }

        [Theory]
        [InlineData("任务A", 5, "5 - 任务A")]
        [InlineData("5 - 任务A", 5, "5 - 任务A")]
        [InlineData("任务A", -1, "任务A")]
        [InlineData("", 5, "")]
        public void ApplyPrefix_IsIdempotent(string name, int id, string expected) {
            Assert.Equal(expected, MissionNameHelper.ApplyPrefix(name, id));
        }

        [Theory]
        [InlineData("5 - 任务A", "任务A")]
        [InlineData(" 12 - 任务A", "任务A")]
        [InlineData("任务A", "任务A")]
        [InlineData("5 - 5 - 任务A", "5 - 任务A")]
        [InlineData("123-456", "123-456")]
        [InlineData("", "")]
        public void BaseName_StripsAnyLeadingIdPrefix(string name, string expected) {
            Assert.Equal(expected, MissionNameHelper.BaseName(name));
        }

        [Theory]
        [InlineData("5 - 任务A", "任务A", true)]
        [InlineData("任务A", "5 - 任务A", true)]
        [InlineData("5 - 任务A", "7 - 任务A", true)]
        [InlineData("5 - 任务A", "任务B", false)]
        [InlineData("5 - ABC", "abc", true)]
        [InlineData("123-456", "7 - 456", false)]
        [InlineData(null, null, true)]
        [InlineData(null, "任务A", false)]
        [InlineData("", "任务A", false)]
        public void NameEquals_IgnoresIdPrefix(string? a, string? b, bool expected) {
            Assert.Equal(expected, MissionNameHelper.NameEquals(a, b));
        }
    }
}
