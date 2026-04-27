using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using CoreEvents.Data.Repositories;
using CoreEvents.Models.Domain;
using Moq;
using Newtonsoft.Json.Linq;

namespace CoreEvents.Tests.Repositories
{
    public class InMemoryBookingRepositoryTests
    {
        private readonly InMemoryBookingRepository<Booking> _repository;
        private readonly ConcurrentDictionary<Guid, Booking> _dictionary = new();

        public InMemoryBookingRepositoryTests()
        {
            _repository = new InMemoryBookingRepository<Booking>();
        }

        private Booking SeedOneBooking()
        {
            var entity = new Booking()
            {
                Id = Guid.NewGuid(),
                EventId = Guid.NewGuid(),
                Status = BookingStatus.Pending,
                CreatedAt = new DateTime(2026, 01, 01, 08, 00, 00)
            };
            _repository.Add(entity, CancellationToken.None);
            return entity;
        }

        [Fact]
        public void GetAll_WithExistingBookingId_ShouldRetrieveEventSuccessfully()
        {
            // Arrange
            var existingBooking1 = SeedOneBooking();
            var existingBooking2 = SeedOneBooking();

            // Act
            var result = _repository.GetAll(CancellationToken.None);

            var list = result.ToList();

            // Assert
            Assert.Contains(list, x => x.Id == existingBooking1.Id);
            Assert.Contains(list, x => x.Id == existingBooking2.Id);
        }

        [Fact]
        public void GetAll_WhenNoItems_ShouldReturnEmptyCollection()
        {
            // Act
            var result = _repository.GetAll(CancellationToken.None);

            // Assert
            Assert.Empty(result);
            Assert.NotNull(result);
        }

        [Fact]
        public void GetById_WithExistingBookingId_ShouldRetrieveEventSuccessfully()
        {
            // Arrange
            var existingBooking = SeedOneBooking();

            // Act
            var result = _repository.GetById(existingBooking.Id, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(existingBooking.Id, result.Id);
        }

        [Fact]
        public void GetById_WhenNonExistingId_ShouldReturnNull()
        {
            // Arrange
            var nonExistingBooking = Guid.NewGuid();

            // Act
            var result = _repository.GetById(nonExistingBooking, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Add_WhenIdIsEmpty_ShouldGenerateNewIdAndAdd()
        {
            // Arrange
            var existingBooking = SeedOneBooking();
            existingBooking.Id = Guid.Empty;

            // Act
            _repository.Add(existingBooking, CancellationToken.None);

            var result = _repository.GetById(existingBooking.Id, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(Guid.Empty, result.Id);
            Assert.Equal(existingBooking.Id, result.Id);
        }

        [Fact]
        public void Add_WhenExistingId_ShouldRetrieveBookingSuccessfully()
        {
            // Arrange
            var existingBooking = SeedOneBooking();

            // Act
            _repository.Add(existingBooking, CancellationToken.None);

            var result = _repository.GetById(existingBooking.Id, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(existingBooking.Id, result.Id);
        }

        [Fact]
        public void Update_WhenExistingIdAndValidUpdateData_ShouldRetrieveBookingSuccessfullyUpdated()
        {
            // Arrange
            var existingBooking = SeedOneBooking();
            var updateBooking = new Booking()
            {
                Id = existingBooking.Id,
                EventId = Guid.NewGuid(),
                CreatedAt = new DateTime(2026, 04, 27, 12, 00, 00),
                Status = BookingStatus.Confirmed,
                ProcessedAt = new DateTime(2026, 04, 27, 12, 15, 00)
            };

            // Act
            _repository.Update(updateBooking, CancellationToken.None);
            var result = _repository.GetById(existingBooking.Id, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(updateBooking.Id, result.Id);
            Assert.Equal(updateBooking.EventId, result.EventId);
            Assert.Equal(updateBooking.CreatedAt, result.CreatedAt);
            Assert.Equal(updateBooking.ProcessedAt, result.ProcessedAt);
            Assert.Equal(updateBooking.Status, result.Status);
        }

        [Fact]
        public void Update_WhenNonExistingId_ShouldDoNothing()
        {
            // Arrange
            var nonExistingBooking = SeedOneBooking();
            nonExistingBooking.Id = Guid.NewGuid();

            // Act
            _repository.Update(nonExistingBooking, CancellationToken.None);
            var result = _repository.GetById(nonExistingBooking.Id, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Delete_WhenExistingId_ShouldRemove()
        {
            // Arrange
            var nonExistingBooking = SeedOneBooking();

            // Act
            _repository.Delete(nonExistingBooking.Id, CancellationToken.None);
            var result = _repository.GetById(nonExistingBooking.Id, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Delete_WhenNonExistingId_ShouldDoNothing()
        {
            // Arrange
            var nonExistingBooking = SeedOneBooking();
            nonExistingBooking.Id = Guid.NewGuid();

            // Act
            _repository.Delete(nonExistingBooking.Id, CancellationToken.None);
            var result = _repository.GetById(nonExistingBooking.Id, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetAll_WhenCancellationRequested_ShouldThrowOperationCanceledException()
        {
            // Arrange
            string expectedExceptionMessage = $"The operation was canceled.";
            var cancellationToken = new CancellationTokenSource();
            cancellationToken.Cancel();

            // Act
            var exception = Assert.Throws<OperationCanceledException>(() => _repository.GetAll(cancellationToken.Token));

            // Assert
            Assert.Equal(expectedExceptionMessage, exception.Message);
            Assert.Equal(cancellationToken.Token, exception.CancellationToken);
        }

        [Fact]
        public void GetById_WhenCancellationRequested_ShouldThrowOperationCanceledException()
        {
            // Arrange
            var existingBooking = Guid.NewGuid();
            string expectedExceptionMessage = $"The operation was canceled.";
            var cancellationToken = new CancellationTokenSource();
            cancellationToken.Cancel();

            // Act
            var exception = Assert.Throws<OperationCanceledException>(() => _repository.GetById(existingBooking, cancellationToken.Token));

            // Assert
            Assert.Equal(expectedExceptionMessage, exception.Message);
            Assert.Equal(cancellationToken.Token, exception.CancellationToken);
        }

        [Fact]
        public void Add_WhenCancellationRequested_ShouldThrowOperationCanceledException()
        {
            // Arrange
            var existBooking = SeedOneBooking();
            string expectedExceptionMessage = $"The operation was canceled.";
            var cancellationToken = new CancellationTokenSource();
            cancellationToken.Cancel();

            // Act
            var exception = Assert.Throws<OperationCanceledException>(() => _repository.Add(existBooking, cancellationToken.Token));

            // Assert
            Assert.Equal(expectedExceptionMessage, exception.Message);
            Assert.Equal(cancellationToken.Token, exception.CancellationToken);
        }

        [Fact]
        public void Delete_WhenCancellationRequested_ShouldThrowOperationCanceledException()
        {
            // Arrange
            var existBooking = SeedOneBooking();
            string expectedExceptionMessage = $"The operation was canceled.";
            var cancellationToken = new CancellationTokenSource();
            cancellationToken.Cancel();

            // Act
            var exception = Assert.Throws<OperationCanceledException>(() => _repository.Delete(existBooking.Id, cancellationToken.Token));

            // Assert
            Assert.Equal(expectedExceptionMessage, exception.Message);
            Assert.Equal(cancellationToken.Token, exception.CancellationToken);
        }

        [Fact]
        public void Update_WhenCancellationRequested_ShouldThrowOperationCanceledException()
        {
            // Arrange
            var existBooking = SeedOneBooking();
            string expectedExceptionMessage = $"The operation was canceled.";
            var cancellationToken = new CancellationTokenSource();
            cancellationToken.Cancel();

            // Act
            var exception = Assert.Throws<OperationCanceledException>(() => _repository.Update(existBooking, cancellationToken.Token));

            // Assert
            Assert.Equal(expectedExceptionMessage, exception.Message);
            Assert.Equal(cancellationToken.Token, exception.CancellationToken);
        }

    }
}
