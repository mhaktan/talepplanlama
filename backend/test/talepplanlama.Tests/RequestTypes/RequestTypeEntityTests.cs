using System;
using Xunit;
using FluentAssertions;
using talepplanlama.Entities;

namespace talepplanlama.Tests.RequestTypes
{
    public class RequestTypeEntityTests
    {
        [Fact]
        public void RequestType_ShouldBeCreatable()
        {
            // Act
            var entity = new RequestType();

            // Assert
            entity.Should().NotBeNull();
        }

        [Fact]
        public void RequestType_ShouldHaveDefaultValues()
        {
            // Act
            var entity = new RequestType();

            // Assert
            entity.Id.Should().Be(default(long));

        }

        [Fact]
        public void RequestType_Name_ShouldAcceptValue()
        {
            var entity = new RequestType { Name = "Test Value" };
            entity.Name.Should().Be("Test Value");
        }

    }
}
