using System;
using Xunit;
using FluentAssertions;
using talepplanlama.Entities;

namespace talepplanlama.Tests.ChangeRequests
{
    public class ChangeRequestEntityTests
    {
        [Fact]
        public void ChangeRequest_ShouldBeCreatable()
        {
            // Act
            var entity = new ChangeRequest();

            // Assert
            entity.Should().NotBeNull();
        }

        [Fact]
        public void ChangeRequest_ShouldHaveDefaultValues()
        {
            // Act
            var entity = new ChangeRequest();

            // Assert
            entity.Id.Should().Be(default(long));

        }

        [Fact]
        public void ChangeRequest_Title_ShouldAcceptValue()
        {
            var entity = new ChangeRequest { Title = "Test Value" };
            entity.Title.Should().Be("Test Value");
        }

        [Fact]
        public void ChangeRequest_Description_ShouldAcceptValue()
        {
            var entity = new ChangeRequest { Description = "Test Value" };
            entity.Description.Should().Be("Test Value");
        }

        [Fact]
        public void ChangeRequest_FirstApproverRole_ShouldAcceptValue()
        {
            var entity = new ChangeRequest { FirstApproverRole = "Test Value" };
            entity.FirstApproverRole.Should().Be("Test Value");
        }

        [Fact]
        public void ChangeRequest_SecondApproverRole_ShouldAcceptValue()
        {
            var entity = new ChangeRequest { SecondApproverRole = "Test Value" };
            entity.SecondApproverRole.Should().Be("Test Value");
        }

    }
}
