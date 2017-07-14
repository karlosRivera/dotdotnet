﻿using System;
using System.IO;
using System.Text;
using Dot.Net.DevFast.Extensions.StreamExt;
using Dot.Net.DevFast.Extensions.StringExt;
using Dot.Net.DevFast.Tests.TestHelpers;
using NUnit.Framework;

namespace Dot.Net.DevFast.Tests.Extensions.StreamExt
{
    [TestFixture]
    public class Base64ExtTest
    {
        [Test]
        [TestCase(null)]
        [TestCase("utf-8")]
        [TestCase("utf-7")]
        [TestCase("utf-32BE")]
        [TestCase("utf-32")]
        [TestCase("utf-16BE")]
        [TestCase("utf-16")]
        [TestCase("us-ascii")]
        public void String_Based_ToBase64_Works_Correctly(string enc)
        {
            var extB64 = TestValues.BigString.ToBase64(Base64FormattingOptions.InsertLineBreaks,
                Encoding.GetEncoding(enc.TrimSafeOrDefault("utf-8")));
            var localB64 = Convert.ToBase64String(
                    Encoding.GetEncoding(enc.TrimSafeOrDefault("utf-8")).GetBytes(TestValues.BigString),
                    Base64FormattingOptions.InsertLineBreaks);

            Assert.True(extB64.Equals(localB64));
        }

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(350)]
        public void ByteArray_Based_ToBase64_Works_Correctly(int arrSize)
        {
            var randm = new Random();
            var bytes = new byte[arrSize];
            randm.NextBytes(bytes);

            var extB64 = bytes.ToBase64(Base64FormattingOptions.InsertLineBreaks);
            var localB64 = Convert.ToBase64String(bytes, Base64FormattingOptions.InsertLineBreaks);

            Assert.True(extB64.Equals(localB64));
        }

        [Test]
        [TestCase(0, 0)]
        [TestCase(1, 0)]
        [TestCase(1, 1)]
        [TestCase(2, 0)]
        [TestCase(2, 1)]
        [TestCase(2, 2)]
        [TestCase(350, 123)]
        [TestCase(99, 91)]
        public void ByteArraySegment_Based_ToBase64_Works_Correctly(int arrSize, int segSize)
        {
            var randm = new Random();
            var bytes = new byte[arrSize];
            randm.NextBytes(bytes);

            var segment = new ArraySegment<byte>(bytes, 0, segSize);
            var extB64 = segment.ToBase64(Base64FormattingOptions.InsertLineBreaks);
            var localB64 = Convert.ToBase64String(bytes, 0, segSize, Base64FormattingOptions.InsertLineBreaks);

            Assert.True(extB64.Equals(localB64));
        }

        [Test]
        [TestCase(null)]
        [TestCase("utf-8")]
        [TestCase("utf-7")]
        [TestCase("utf-32BE")]
        [TestCase("utf-32")]
        [TestCase("utf-16BE")]
        [TestCase("utf-16")]
        [TestCase("us-ascii")]
        public void String_Returning_FromBase64_Works_Correctly(string enc)
        {
            var encIns = Encoding.GetEncoding(enc.TrimSafeOrDefault("utf-8"));
            var extB64 = TestValues.BigString.ToBase64(Base64FormattingOptions.InsertLineBreaks, encIns);
            var resultStr = extB64.FromBase64(encIns);
            Assert.True(resultStr.Equals(TestValues.BigString));
        }

        [Test]
        [TestCase(null)]
        [TestCase("utf-8")]
        [TestCase("utf-7")]
        [TestCase("utf-32BE")]
        [TestCase("utf-32")]
        [TestCase("utf-16BE")]
        [TestCase("utf-16")]
        [TestCase("us-ascii")]
        public void String_Returning_FromBase64_Detects_Encoding_From_ByteMark_When_Null_Encoding_Is_Given(string enc)
        {
            using (var buff = new MemoryStream())
            {
                var encIns = Encoding.GetEncoding(enc.TrimSafeOrDefault("utf-8"));
                var dataArr = encIns.GetPreamble();
                buff.Write(dataArr, 0, dataArr.Length);
                dataArr = encIns.GetBytes(TestValues.BigString);
                buff.Write(dataArr, 0, dataArr.Length);

                var extB64 = new ArraySegment<byte>(buff.GetBuffer(), 0, (int) buff.Length).ToBase64();
                var resultStr = extB64.FromBase64(null);
                Assert.True(TestValues.BigString.Equals(resultStr));
            }
        }

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(350)]
        public void ByteArray_Returning_FromBase64_Works_Correctly(int arrSize)
        {
            var randm = new Random();
            var bytes = new byte[arrSize];
            randm.NextBytes(bytes);

            var extB64 = bytes.ToBase64(Base64FormattingOptions.InsertLineBreaks);
            var returnArr = extB64.FromBase64();

            Assert.NotNull(returnArr);
            Assert.True(returnArr.Length.Equals(bytes.Length));
            for (var i = 0; i < bytes.Length; i++)
            {
                Assert.True(returnArr[i].Equals(bytes[i]));
            }
        }
    }
}