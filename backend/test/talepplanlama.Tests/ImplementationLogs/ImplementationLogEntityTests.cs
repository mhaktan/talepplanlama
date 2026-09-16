using System;
using Xunit;
using FluentAssertions;
using talepplanlama.Entities;

namespace talepplanlama.Tests.ImplementationLogs
{
    public class ImplementationLogEntityTests
    {
        [Fact]
        public void ImplementationLog_ShouldBeCreatable()
        {
            // Act
            var entity = new ImplementationLog();

            // Assert
            entity.Should().NotBeNull();
        }

        [Fact]
        public void ImplementationLog_ShouldHaveDefaultValues()
        {
            // Act
            var entity = new ImplementationLog();

            // Assert
            entity.Id.Should().Be(default(long));

        }


    }
}
