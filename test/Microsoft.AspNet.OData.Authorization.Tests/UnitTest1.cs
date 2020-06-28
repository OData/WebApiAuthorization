using System;
using Xunit;
using Microsoft.AspNet.OData.Authorization;

namespace Microsoft.AspNet.OData.Authorization.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            var test = new Class1();
            Assert.Equal(3, test.Add(1, 2));
        }
    }
}
