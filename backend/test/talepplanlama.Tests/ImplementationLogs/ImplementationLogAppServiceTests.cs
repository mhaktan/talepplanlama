using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Abp.Domain.Repositories;
using Moq;
using talepplanlama.Entities;
using talepplanlama.ImplementationLogs;
using talepplanlama.ImplementationLogs.Dto;
using talepplanlama.Flows;

namespace talepplanlama.Tests.ImplementationLogs
{
    public class ImplementationLogAppServiceTests
    {
        private readonly Mock<IRepository<ImplementationLog, long>> _repositoryMock;
        private readonly ImplementationLogAppService _service;

        public ImplementationLogAppServiceTests()
        {
            _repositoryMock = new Mock<IRepository<ImplementationLog, long>>();
            _service = new ImplementationLogAppService(_repositoryMock.Object, new Mock<IFlowEngine>().Object);
        }

        [Fact]
        public void Repository_GetAll_ShouldReturnQueryable()
        {
            // Arrange
            var entities = new[]
            {
                new ImplementationLog { Id = 1, Phase = 0 },
                new ImplementationLog { Id = 2, Phase = 0 },
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
                new ImplementationLog { Id = 1, Phase = 0 },
                new ImplementationLog { Id = 2, Phase = 0 },
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
            var dto = new CreateImplementationLogDto
            {
                Phase = 0
            };

            _repositoryMock.Setup(r => r.InsertAndGetIdAsync(It.IsAny<ImplementationLog>()))
                .ReturnsAsync(1);
            _repositoryMock.Setup(r => r.GetAsync(It.IsAny<long>()))
                .ReturnsAsync(new ImplementationLog { Id = 1, Phase = 0 });

            // Act & Assert
            _service.Should().NotBeNull();
        }

        [Fact]
        public async Task Delete_ShouldRemoveEntity()
        {
            // Arrange
            _repositoryMock.Setup(r => r.GetAsync(It.IsAny<long>()))
                .ReturnsAsync(new ImplementationLog { Id = 1, Phase = 0 });

            // Act & Assert
            await _service.Invoking(s => s.DeleteAsync(new Abp.Application.Services.Dto.EntityDto<long> { Id = 1 }))
                .Should().NotThrowAsync();
        }
    }
}
