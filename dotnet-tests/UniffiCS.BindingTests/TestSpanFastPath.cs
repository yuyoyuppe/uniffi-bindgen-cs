/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Text;
using uniffi.span_fast_path;

namespace UniffiCS.BindingTests;

public class TestSpanFastPath
{
    [Fact]
    public void StringRoundTripsThroughDelegatingOverload()
    {
        Assert.Equal("héllo wörld 🚀", SpanFastPathMethods.SpanEchoString("héllo wörld 🚀"));
    }

    [Fact]
    public void StringRoundTripsThroughSpanOverload()
    {
        ReadOnlySpan<byte> utf8 = Encoding.UTF8.GetBytes("héllo wörld 🚀");
        Assert.Equal("héllo wörld 🚀", SpanFastPathMethods.SpanEchoStringSpan(utf8));
    }

    [Fact]
    public void BytesRoundTripThroughBothOverloads()
    {
        byte[] payload = { 0, 1, 2, 254, 255 };
        Assert.Equal(payload, SpanFastPathMethods.SpanEchoBytes(payload));
        Assert.Equal(payload, SpanFastPathMethods.SpanEchoBytesSpan(payload));
    }

    [Fact]
    public void EmptySpansCross()
    {
        Assert.Equal("", SpanFastPathMethods.SpanEchoStringSpan(ReadOnlySpan<byte>.Empty));
        Assert.Equal(new byte[0], SpanFastPathMethods.SpanEchoBytesSpan(ReadOnlySpan<byte>.Empty));
    }

    [Fact]
    public void MixedSpanAndStandardArguments()
    {
        Assert.Equal(
            "PRE:7:3",
            SpanFastPathMethods.SpanDescribeSpan(Encoding.UTF8.GetBytes("pre"), 7, new byte[] { 1, 2, 3 }, true));
        Assert.Equal("pre:7:3", SpanFastPathMethods.SpanDescribe("pre", 7, new byte[] { 1, 2, 3 }, false));
    }

    [Fact]
    public void ErrorsPropagateThroughSpanPath()
    {
        var e1 = Assert.Throws<SpanException.Boom>(() => SpanFastPathMethods.SpanFail("kaboom"));
        Assert.Equal("kaboom", e1.message);
        var e2 = Assert.Throws<SpanException.Boom>(
            () => SpanFastPathMethods.SpanFailSpan(Encoding.UTF8.GetBytes("kaboom")));
        Assert.Equal("kaboom", e2.message);
    }

    [Fact]
    public void InvalidUtf8ThroughSpanOverloadThrows()
    {
        byte[] invalid = { 0xFF, 0xFE, 0xFD };
        Assert.ThrowsAny<UniffiException>(() => SpanFastPathMethods.SpanEchoStringSpan(invalid));
    }

    [Fact]
    public void MethodSpanVariantsShareTheReceiver()
    {
        var recorder = new SpanRecorder();
        recorder.Append("a");
        recorder.AppendSpan(Encoding.UTF8.GetBytes("b"));
        recorder.AppendBytes(new byte[] { 1, 2 });
        recorder.AppendBytesSpan(new byte[] { 3, 4, 5 });
        Assert.Equal("ab[2 bytes][3 bytes]", recorder.Value());

        var e = Assert.Throws<SpanException.Boom>(() => recorder.FailSpan(Encoding.UTF8.GetBytes("nope")));
        Assert.Equal("nope", e.message);
    }
}
