using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectManagementSystem.Controllers;
using ProjectManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectManagementSystem.UnitTests.Controller
{
    public class HomeControllerTest
    {
        private readonly Mock<ILogger<HomeController>> _loggerMock;
        private readonly HomeController _controller;

        public HomeControllerTest()
        {
            _loggerMock = new Mock<ILogger<HomeController>>();
            _controller = new HomeController(_loggerMock.Object);
        }


        [Fact]
        public void Index_ReturnsViewResult()
        {
            // Act
            var result = _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewName); // By default, the view name is null, which means it returns the view with the same name as the action
        }

        [Fact]
        public void Privacy_ReturnsViewResult()
        {
            // Act
            var result = _controller.Privacy();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewName); // By default, the view name is null, which means it returns the view with the same name as the action
        }

        [Fact]
        public void Error_ReturnsViewResult_WithErrorViewModel()
        {
            // Arrange
            var expectedRequestId = "TestRequestId";
            var httpContext = new DefaultHttpContext();
            httpContext.TraceIdentifier = expectedRequestId;
            _controller.ControllerContext.HttpContext = httpContext;

            // Act
            var result = _controller.Error();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ErrorViewModel>(viewResult.Model);
            Assert.Equal(expectedRequestId, model.RequestId);
        }

        [Fact]
        public void Error_ReturnsViewResult_WithErrorViewModel_WhenActivityIsNull()
        {
            // Arrange
            Activity.Current = null;
            var expectedRequestId = "TestRequestId";
            var httpContext = new DefaultHttpContext();
            httpContext.TraceIdentifier = expectedRequestId;
            _controller.ControllerContext.HttpContext = httpContext;

            // Act
            var result = _controller.Error();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ErrorViewModel>(viewResult.Model);
            Assert.Equal(expectedRequestId, model.RequestId);
        }

    }
}
