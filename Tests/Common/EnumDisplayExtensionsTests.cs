using Phycock.Common;
using Phycock.Entity.Enums;
using Xunit;

namespace Tests.Common
{
    public class EnumDisplayExtensionsTests
    {
        [Fact]
        public void GetDisplayName_ReturnsDisplayAttributeName()
        {
            var result = RecordTiming.Noon.GetDisplayName();

            Assert.Equal("訓練開始時", result);
        }

        [Fact]
        public void GetDisplayName_ReturnsEnumName_WhenDisplayAttributeIsMissing()
        {
            var result = DayOfWeek.Monday.GetDisplayName();

            Assert.Equal("Monday", result);
        }
    }
}
