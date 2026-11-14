using Tracking.Contracts;

namespace Tracking.Api.Tests;

public class TrackingCodeGeneratorTests
{
    [Fact]
    public void Generate_SameInstantDifferentOffsets_UsesUtcDate()
    {
        // 23:30 UTC ngày 14/11/2026
        var instant = new DateTimeOffset(2026, 11, 14, 23, 30, 0, TimeSpan.Zero);

        // Cùng một khoảnh khắc, hiển thị theo hai offset khác nhau
        var minus5 = instant.ToOffset(TimeSpan.FromHours(-5)); // 18:30 ngày 14
        var plus7 = instant.ToOffset(TimeSpan.FromHours(7));  // 06:30 ngày 15

        string codeA = TrackingCodeGenerator.Generate(minus5);
        string codeB = TrackingCodeGenerator.Generate(plus7);

        // "VN" + 6 chữ số ngày = 8 ký tự đầu
        Assert.Equal("VN261114", codeA[..8]);
        Assert.Equal("VN261114", codeB[..8]);
    }

    [Fact]
    public void Generate_ProducesValidAndUniqueCodes()
    {
        var now = DateTimeOffset.UtcNow;
        var seen = new HashSet<string>();

        for (int i = 0; i < 1000; i++)
        {
            var code = TrackingCodeGenerator.Generate(now);

            Assert.True(TrackingCodeGenerator.TryNormalize(code, out var normalized));
            Assert.Equal(code, normalized);
            seen.Add(code);
        }

        Assert.Equal(1000, seen.Count);
    }

    [Theory]
    [InlineData("VN261114ABC234")]
    [InlineData("vn261114abc234")]
    [InlineData("  vn261114abc234  ")]
    public void TryNormalize_ValidInput_ReturnsUpperCaseCode(string input)
    {
        Assert.True(TrackingCodeGenerator.TryNormalize(input, out var normalized));
        Assert.Equal("VN261114ABC234", normalized);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("VN261114ABC23")]    // thiếu 1 ký tự
    [InlineData("VN261114ABC2345")]  // thừa 1 ký tự
    [InlineData("XX261114ABC234")]   // sai tiền tố
    [InlineData("VN261114ABCDEI")]   // chứa I
    [InlineData("VN261114ABC230")]   // chứa 0
    [InlineData("VN261114ABC231")]   // chứa 1
    [InlineData("VN\u0662\u0666\u0661\u0661\u0661\u0664ABC234")] // chữ số Ả Rập
    public void TryNormalize_InvalidInput_ReturnsFalse(string? input)
    {
        Assert.False(TrackingCodeGenerator.TryNormalize(input, out var normalized));
        Assert.Equal(string.Empty, normalized);
    }
}