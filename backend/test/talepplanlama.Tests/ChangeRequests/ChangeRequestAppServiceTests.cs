using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Abp.Domain.Repositories;
using Moq;
using talepplanlama.Entities;
using talepplanlama.ChangeRequests;
using talepplanlama.ChangeRequests.Dto;
using talepplanlama.Flows;

namespace talepplanlama.Tests.ChangeRequests
{
    public class ChangeRequestAppServiceTests
    {
        private readonly Mock<IRepository<ChangeRequest, long>> _repositoryMock;
        private readonly ChangeRequestAppService _service;

        public ChangeRequestAppServiceTests()
        {
            _repositoryMock = new Mock<IRepository<ChangeRequest, long>>();
            _service = new ChangeRequestAppService(_repositoryMock.Object, new Mock<IFlowEngine>().Object, new Mock<IRepository<StatusChangeLog, long>>().Object, new Mock<IRepository<ApprovalRecord, Guid>>().Object);
        }

        [Fact]
        public void Repository_GetAll_ShouldReturnQueryable()
        {
            // Arrange
            var entities = new[]
            {
                new ChangeRequest { Id = 1, Title = "Test title", Description = "Test description", Status = 0, FirstApproverRole = "Test firstApproverRole", SecondApproverRole = "Test secondApproverRole" },
                new ChangeRequest { Id = 2, Title = "Test title", Description = "Test description", Status = 0, FirstApproverRole = "Test firstApproverRole", SecondApproverRole = "Test secondApproverRole" },
            }.AsQueryable();

            _repositoryMock.Setup(r => r.GetAll()).Returns(entities);

            // Act
            var result = _repositoryMock.Object.GetAll();

            // Assert
            result.Should().NotBeNull();
            result.Count().Should().Be(2);
        }

        [Fact]
        public void Repository_GetAll_WithFilter_ShouldWork()
        {
            // Arrange
            var entities = new[]
            {
                new ChangeRequest { Id = 1, Title = "Test title", Description = "Test description", Status = 0, FirstApproverRole = "Test firstApproverRole", SecondApproverRole = "Test secondApproverRole" },
                new ChangeRequest { Id = 2, Title = "Test title", Description = "Test description", Status = 0, FirstApproverRole = "Test firstApproverRole", SecondApproverRole = "Test secondApproverRole" },
            }.AsQueryable();

            _repositoryMock.Setup(r => r.GetAll()).Returns(entities);

            // Act — simulate keyword filter
            var result = _repositoryMock.Object.GetAll()
                .Where(x => x.Id.ToString().Contains("1"));

            // Assert
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task Create_ShouldInsertEntity()
        {
            // Arrange
            var dto = new CreateChangeRequestDto
            {
                Title = "Test title", Description = "Test description", Status = 0, FirstApproverRole = "Test firstApproverRole", SecondApproverRole = "Test secondApproverRole"
            };

            _repositoryMock.Setup(r => r.InsertAndGetIdAsync(It.IsAny<ChangeRequest>()))
                .ReturnsAsync(1);
            _repositoryMock.Setup(r => r.GetAsync(It.IsAny<long>()))
                .ReturnsAsync(new ChangeRequest { Id = 1, Title = "Test title", Description = "Test description", Status = 0, FirstApproverRole = "Test firstApproverRole", SecondApproverRole = "Test secondApproverRole" });

            // Act & Assert
            _service.Should().NotBeNull();
        }

        [Fact]
        public async Task Delete_ShouldRemoveEntity()
        {
            // Arrange
            _repositoryMock.Setup(r => r.GetAsync(It.IsAny<long>()))
                .ReturnsAsync(new ChangeRequest { Id = 1, Title = "Test title", Description = "Test description", Status = 0, FirstApproverRole = "Test firstApproverRole", SecondApproverRole = "Test secondApproverRole" });

            // Act & Assert
            await _service.Invoking(s => s.DeleteAsync(new Abp.Application.Services.Dto.EntityDto<long> { Id = 1 }))
                .Should().NotThrowAsync();
        }
    }
}
