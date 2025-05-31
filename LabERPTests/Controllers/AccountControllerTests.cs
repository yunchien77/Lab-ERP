using LabERP.Interface;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using LabERP.Controllers;
using LabERP.Interface;
using LabERP.Models.Core;
using LabERP.Models.Handlers;
using LabERP.Models.ViewModels;
using Xunit;

namespace LabERPTests.Controllers
{
    public class AccountControllerTests
    {
        private readonly Mock<IAccountHandler> _mockAccountHandler;
        private readonly AccountController _controller;
        private readonly Mock<IAuthenticationService> _mockAuthService;
        private readonly Mock<IServiceProvider> _mockServiceProvider;

        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly AccountHandler _accountHandler;

        public AccountControllerTests()
        {
            // 初始化 _mockUserRepository
            _mockUserRepository = new Mock<IUserRepository>();

            // 創建 IAccountHandler 的模擬
            _mockAccountHandler = new Mock<IAccountHandler>();

            // 設置驗證服務模擬
            _mockAuthService = new Mock<IAuthenticationService>();
            _mockServiceProvider = new Mock<IServiceProvider>();

            // 建立控制器，使用模擬的 IAccountHandler
            _controller = new AccountController(_mockAccountHandler.Object);

            // 模擬 HttpContext
            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = _mockServiceProvider.Object;

            // 為控制器提供 TempData
            _controller.TempData = new TempDataDictionary(
                httpContext,
                Mock.Of<ITempDataProvider>());

            // 設置控制器的 ControllerContext
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // 設置模擬服務提供者返回驗證服務
            _mockServiceProvider
                .Setup(sp => sp.GetService(typeof(IAuthenticationService)))
                .Returns(_mockAuthService.Object);

            // 模擬 IUrlHelper
            var mockUrlHelper = new Mock<IUrlHelper>();
            mockUrlHelper
                .Setup(u => u.Action(It.IsAny<UrlActionContext>()))
                .Returns("mockedUrl");

            _controller.Url = mockUrlHelper.Object;
        }

        [Fact]
        public void Login_Get_ReturnsViewResult()
        {
            // Act
            var result = _controller.Login();

            // Assert
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task Login_Post_ValidCredentials_RedirectsToDashboard()
        {
            // Arrange
            var model = new LoginViewModel
            {
                Username = "testUser",
                Password = "password123",
                RememberMe = false
            };

            var user = new User
            {
                UserID = "123",
                Username = "testUser",
                Email = "test@example.com",
                Role = "User"
            };

            _mockAccountHandler
                .Setup(m => m.AuthenticateUser(model.Username, model.Password))
                .Returns(user);

            _mockAuthService
                .Setup(auth => auth.SignInAsync(
                    It.IsAny<HttpContext>(),
                    It.IsAny<string>(),
                    It.IsAny<ClaimsPrincipal>(),
                    It.IsAny<AuthenticationProperties>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Login(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Dashboard", redirectResult.ActionName);
            Assert.Equal("User", redirectResult.ControllerName);
        }

        [Fact]
        public async Task Login_Post_ValidCredentials_WithRememberMe_SetsPersistentCookie()
        {
            // Arrange
            var model = new LoginViewModel
            {
                Username = "testUser",
                Password = "password123",
                RememberMe = true
            };

            var user = new User
            {
                UserID = "123",
                Username = "testUser",
                Email = "test@example.com",
                Role = "User"
            };

            _mockAccountHandler
                .Setup(m => m.AuthenticateUser(model.Username, model.Password))
                .Returns(user);

            _mockAuthService
                .Setup(auth => auth.SignInAsync(
                    It.IsAny<HttpContext>(),
                    It.IsAny<string>(),
                    It.IsAny<ClaimsPrincipal>(),
                    It.Is<AuthenticationProperties>(p => p.IsPersistent == true)))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Login(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Dashboard", redirectResult.ActionName);

            // 驗證驗證服務是否被調用
            _mockAuthService.Verify(
                auth => auth.SignInAsync(
                    It.IsAny<HttpContext>(),
                    It.IsAny<string>(),
                    It.IsAny<ClaimsPrincipal>(),
                    It.Is<AuthenticationProperties>(p => p.IsPersistent == true)),
                Times.Once);
        }

        [Fact]
        public async Task Login_Post_InvalidCredentials_ReturnsViewWithModel()
        {
            // Arrange
            var model = new LoginViewModel
            {
                Username = "wrongUser",
                Password = "wrongPassword",
                RememberMe = false
            };

            _mockUserRepository
                .Setup(m => m.FindUser(model.Username, model.Password))
                .Returns((User)null);

            _mockAuthService
                .Setup(auth => auth.SignInAsync(
                    It.IsAny<HttpContext>(),
                    It.IsAny<string>(),
                    It.IsAny<ClaimsPrincipal>(),
                    It.IsAny<AuthenticationProperties>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Login(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Same(model, viewResult.Model);
            Assert.True(_controller.ModelState.ContainsKey(""));
            Assert.Equal("使用者名稱或密碼不正確", _controller.ModelState[""].Errors[0].ErrorMessage);
        }

        [Fact]
        public async Task Login_Post_InvalidModel_ReturnsViewWithModel()
        {
            // Arrange
            var model = new LoginViewModel();
            _controller.ModelState.AddModelError("Username", "用戶名是必需的");

            // Act
            var result = await _controller.Login(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Same(model, viewResult.Model);
        }

        [Fact]
        public async Task Logout_SignsOutAndRedirectsToHome()
        {
            // Arrange
            _mockAuthService
                .Setup(auth => auth.SignOutAsync(
                    It.IsAny<HttpContext>(),
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    It.IsAny<AuthenticationProperties>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Logout();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Home", redirectResult.ControllerName);

            // 驗證驗證服務是否被調用
            _mockAuthService.Verify(
                auth => auth.SignOutAsync(
                    It.IsAny<HttpContext>(),
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    null),
                Times.Once);
        }
    }
}