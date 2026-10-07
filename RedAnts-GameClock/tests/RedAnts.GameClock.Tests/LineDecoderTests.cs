using System.Text;
using RedAnts.GameClock.Clocks;
using Xunit;

namespace RedAnts.GameClock.Tests;

public class LineDecoderTests
{
    const string Sample = "19:38;2;6;3;;;;;0;0;RED;UBO;GAME TIME";

    [Fact]
    public void DetectsTheUtf32BigEndianOfTheHallClock()
    {
        var bytes = new UTF32Encoding(bigEndian: true, byteOrderMark: false).GetBytes(Sample);

        Assert.Equal(LineDecoder.Utf32Be, LineDecoder.Detect(bytes));
        Assert.Equal(Sample, LineDecoder.Decode(bytes, LineDecoder.Auto));
    }

    [Fact]
    public void DetectsUtf16BigEndian()
    {
        var bytes = new UnicodeEncoding(bigEndian: true, byteOrderMark: false).GetBytes(Sample);

        Assert.Equal(LineDecoder.Utf16Be, LineDecoder.Detect(bytes));
        Assert.Equal(Sample, LineDecoder.Decode(bytes, LineDecoder.Auto));
    }

    [Fact]
    public void DetectsPlainAscii()
    {
        var bytes = Encoding.ASCII.GetBytes(Sample);

        Assert.Equal(LineDecoder.Utf8, LineDecoder.Detect(bytes));
        Assert.Equal(Sample, LineDecoder.Decode(bytes, LineDecoder.Auto));
    }

    [Fact]
    public void FallsBackToLatin1ForBytesThatAreNotUtf8()
    {
        var bytes = Encoding.Latin1.GetBytes("Köniz;1;0");

        Assert.Equal(LineDecoder.Latin1, LineDecoder.Detect(bytes));
        Assert.Equal("Köniz;1;0", LineDecoder.Decode(bytes, LineDecoder.Auto));
    }

    [Fact]
    public void StripsPaddingAndLineEndings()
    {
        var bytes = Encoding.UTF8.GetBytes("19:38;2;6\r\n\0\0");

        Assert.Equal("19:38;2;6", LineDecoder.Decode(bytes, LineDecoder.Utf8));
    }

    [Fact]
    public void HandlesAnEmptyDatagram() => Assert.Equal("", LineDecoder.Decode([], LineDecoder.Auto));
}
