using LabERP.Interface;
using Moq;
using LabERP.Models.Core;
using LabERP.Models.Handlers;
using Xunit;

namespace LabERPTests.Models.Handlers
{
    public class AccountHandlerTests
    {
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly AccountHandler _accountHandler;

        public AccountHandlerTests()
        {
            // 設置Mock對象
            _mockUserRepository = new Mock<IUserRepository>();
            _accountHandler = new AccountHandler(_mockUserRepository.Object);
        }

        [Fact]
        public void AuthenticateUser_ValidCredentials_ReturnsUser()
        {
            // Arrange
            string username = "testuser";
            string password = "password123";
            var expectedUser = new User { UserID = "1", Username = username };

            _mockUserRepository
                .Setup(repo => repo.FindUser(username, password))
                .Returns(expectedUser);

            // Act
            var result = _accountHandler.AuthenticateUser(username, password);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedUser.UserID, result.UserID);
            Assert.Equal(expectedUser.Username, result.Username);
            _mockUserRepository.Verify(repo => repo.FindUser(username, password), Times.Once);
        }

        [Fact]
        public void AuthenticateUser_InvalidCredentials_ReturnsNull()
        {
            // Arrange
            string username = "wronguser";
            string password = "wrongpassword";

            _mockUserRepository
                .Setup(repo => repo.FindUser(username, password))
                .Returns((User)null);

            // Act
            var result = _accountHandler.AuthenticateUser(username, password);

            // Assert
            Assert.Null(result);
            _mockUserRepository.Verify(repo => repo.FindUser(username, password), Times.Once);
        }

        [Fact]
        public void RegisterUser_ValidUser_CallsAddOnRepository()
        {
            // Arrange
            var user = new User { UserID = "1", Username = "newuser", Password = "newpassword" };

            // Act
            _accountHandler.RegisterUser(user);

            // Assert
            _mockUserRepository.Verify(repo => repo.Add(user), Times.Once);
        }

        [Fact]
        public void GetUser_ExistingUserId_ReturnsUser()
        {
            // Arrange
            string userId = "1";
            var expectedUser = new User { UserID = userId, Username = "testuser" };

            _mockUserRepository
                .Setup(repo => repo.GetUserById(userId))
                .Returns(expectedUser);

            // Act
            var result = _accountHandler.GetUser(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedUser.UserID, result.UserID);
            Assert.Equal(expectedUser.Username, result.Username);
            _mockUserRepository.Verify(repo => repo.GetUserById(userId), Times.Once);
        }

        [Fact]
        public void GetUser_NonExistingUserId_ReturnsNull()
        {
            // Arrange
            string userId = "999";

            _mockUserRepository
                .Setup(repo => repo.GetUserById(userId))
                .Returns((User)null);

            // Act
            var result = _accountHandler.GetUser(userId);

            // Assert
            Assert.Null(result);
            _mockUserRepository.Verify(repo => repo.GetUserById(userId), Times.Once);
        }
    }
}