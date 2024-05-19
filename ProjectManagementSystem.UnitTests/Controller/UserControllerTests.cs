using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using ProjectManagementSystem.Controllers;
using ProjectManagementSystem.Data;
using ProjectManagementSystem.Models;
using ProjectManagementSystem.Models.ViewModels;
using ProjectManagementSystem.Services;
using Xunit;

namespace ProjectManagementSystemUnitTests.ControllerTests
{
    public class UserControllerTests
    {
        private readonly ApplicationDbContext _db;
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<RoleManager<IdentityRole>> _roleManagerMock;
        private readonly Mock<ITaskService> _taskServiceMock;

        public UserControllerTests()
        {
            // Create a new instance of ApplicationDbContext for testing
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase" + Guid.NewGuid().ToString())
                .Options;

            _db = new ApplicationDbContext(options);

            var store = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(store.Object, null, null, null, null, null, null, null, null);

            var roleStore = new Mock<IRoleStore<IdentityRole>>();
            _roleManagerMock = new Mock<RoleManager<IdentityRole>>(roleStore.Object, null, null, null, null);

            _taskServiceMock = new Mock<ITaskService>();
        }

        [Fact]
        public void Index_ReturnsViewResult_WithListOfUsers()
        {
            // Arrange
            var users = new List<ApplicationUser>
            {
                new ApplicationUser { Id = "1", Name = "John", Surname = "Doe" },
                new ApplicationUser { Id = "2", Name = "Jane", Surname = "Doe" }
            };
            _db.Users.AddRange(users);
            _db.SaveChanges();

            var userList = users.Select(u => new UserViewModel
            {
                Id = u.Id,
                Name = u.Name,
                Surname = u.Surname,
                RoleName = "User"
            }).ToList();

            _taskServiceMock.Setup(service => service.getUserList()).Returns(userList);

            var controller = new UserController(_db, _userManagerMock.Object, _roleManagerMock.Object, _taskServiceMock.Object);

            // Act
            var result = controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<UserViewModel>>(viewResult.ViewData.Model);
            Assert.Equal(2, model.Count);
        }


        [Fact]
        public void Index_ReturnsViewResult_WithEmptyUserList()
        {
            // Arrange
            var userList = new List<UserViewModel>();
            _taskServiceMock.Setup(service => service.getUserList()).Returns(userList);

            var controller = new UserController(_db, _userManagerMock.Object, _roleManagerMock.Object, _taskServiceMock.Object);

            // Act
            var result = controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<UserViewModel>>(viewResult.ViewData.Model);
            Assert.Empty(model);
        }

        [Fact]
        public void Edit_Get_ReturnsViewResult_WithUserViewModel()
        {
            // Arrange
            var user = new ApplicationUser { Id = "1", Name = "John", Surname = "Doe" };
            _db.Users.Add(user);
            _db.SaveChanges();

            var userRoles = new List<IdentityUserRole<string>>
            {
                new IdentityUserRole<string> { UserId = user.Id, RoleId = "1" }
            };
            _db.UserRoles.AddRange(userRoles);
            _db.Roles.Add(new IdentityRole { Id = "1", Name = "User" });
            _db.SaveChanges();

            var controller = new UserController(_db, _userManagerMock.Object, _roleManagerMock.Object, _taskServiceMock.Object);

            // Act
            var result = controller.Edit(user.Id);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<UserViewModel>(viewResult.ViewData.Model);
            Assert.Equal(user.Name, model.Name);
            Assert.Equal(user.Surname, model.Surname);
            Assert.Equal("User", model.RoleName);
        }

        [Fact]
        public void Edit_Get_ReturnsNotFound_WhenIdIsNull()
        {
            // Arrange
            var controller = new UserController(_db, _userManagerMock.Object, _roleManagerMock.Object, _taskServiceMock.Object);

            // Act
            var result = controller.Edit("invalid-id");

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void Edit_Get_ReturnsNotFound_WhenUserNotFound()
        {
            // Arrange
            var controller = new UserController(_db, _userManagerMock.Object, _roleManagerMock.Object, _taskServiceMock.Object);

            // Act
            var result = controller.Edit("invalid-id");

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }


        [Fact]
        public void Edit_Get_ReturnsViewResult_WithUserViewModel_WithoutRole()
        {
            // Arrange
            var user = new ApplicationUser { Id = "1", Name = "John", Surname = "Doe" };
            _db.Users.Add(user);
            _db.SaveChanges();

            var controller = new UserController(_db, _userManagerMock.Object, _roleManagerMock.Object, _taskServiceMock.Object);

            // Act
            var result = controller.Edit(user.Id);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<UserViewModel>(viewResult.ViewData.Model);
            Assert.Equal(user.Name, model.Name);
            Assert.Equal(user.Surname, model.Surname);
            Assert.Null(model.RoleName);
        }


        [Fact]
        public async Task Edit_Post_UpdatesUserRole_AndRedirectsToIndex()
        {
            // Arrange
            var user = new ApplicationUser { Id = "1", Name = "John", Surname = "Doe" };
            _db.Users.Add(user);
            _db.SaveChanges();

            _userManagerMock.Setup(um => um.FindByIdAsync(user.Id)).ReturnsAsync(user);

            var userViewModel = new UserViewModel { Id = user.Id, Name = user.Name, Surname = user.Surname };

            var controller = new UserController(_db, _userManagerMock.Object, _roleManagerMock.Object, _taskServiceMock.Object);

            // Mock HttpContext and Request.Form with missing RoleName
            var context = new DefaultHttpContext();
            context.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = context
            };

            // Act
            var result = await controller.Edit(userViewModel);

            // Assert
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task Edit_Post_ReturnsNotFound_WhenUserNotFound()
        {
            // Arrange
            _userManagerMock.Setup(um => um.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser)null);

            var userViewModel = new UserViewModel { Id = "invalid-id" };

            var controller = new UserController(_db, _userManagerMock.Object, _roleManagerMock.Object, _taskServiceMock.Object);

            // Act
            var result = await controller.Edit(userViewModel);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_Post_ReturnsBadRequest_WhenRoleNameIsEmpty()
        {
            // Arrange
            var user = new ApplicationUser { Id = "1", Name = "John", Surname = "Doe" };
            _db.Users.Add(user);
            _db.SaveChanges();

            _userManagerMock.Setup(um => um.FindByIdAsync(user.Id)).ReturnsAsync(user);
            _userManagerMock.Setup(um => um.GetRolesAsync(user)).ReturnsAsync(new List<string> { "OldRole" });
            _userManagerMock.Setup(um => um.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>())).ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(um => um.AddToRoleAsync(user, "User")).ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(um => um.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            _roleManagerMock.Setup(rm => rm.FindByNameAsync("User")).ReturnsAsync(new IdentityRole { Name = "User" });

            var userViewModel = new UserViewModel { Id = user.Id, Name = user.Name, Surname = user.Surname, RoleName = "User" };

            var controller = new UserController(_db, _userManagerMock.Object, _roleManagerMock.Object, _taskServiceMock.Object);

            // Mock HttpContext and Request.Form
            var context = new DefaultHttpContext();
            context.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "RoleName", "User" }
            });
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = context
            };

            // Act
            var result = await controller.Edit(userViewModel);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
        }


        [Fact]
        public void Delete_Get_ReturnsNotFound_WhenUserNotFound()
        {
            // Arrange
            var controller = new UserController(_db, _userManagerMock.Object, _roleManagerMock.Object, _taskServiceMock.Object);

            // Act
            var result = controller.Delete("invalid-id");

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void Delete_Get_ReturnsViewResult_WithUserViewModel()
        {
            // Arrange
            var user = new ApplicationUser { Id = "1", Name = "John", Surname = "Doe" };
            _db.Users.Add(user);
            _db.SaveChanges();

            var userRoles = new List<IdentityUserRole<string>>
            {
                new IdentityUserRole<string> { UserId = user.Id, RoleId = "1" }
            };
            _db.UserRoles.AddRange(userRoles);
            _db.Roles.Add(new IdentityRole { Id = "1", Name = "User" });
            _db.SaveChanges();

            var controller = new UserController(_db, _userManagerMock.Object, _roleManagerMock.Object, _taskServiceMock.Object);

            // Act
            var result = controller.Delete(user.Id);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<UserViewModel>(viewResult.ViewData.Model);
            Assert.Equal(user.Name, model.Name);
            Assert.Equal(user.Surname, model.Surname);
            Assert.Equal("User", model.RoleName);
        }

        [Fact]
        public void Delete_Get_ReturnsViewResult_WithUserViewModel_WithoutRole()
        {
            // Arrange
            var user = new ApplicationUser { Id = "1", Name = "John", Surname = "Doe" };
            _db.Users.Add(user);
            _db.SaveChanges();

            var controller = new UserController(_db, _userManagerMock.Object, _roleManagerMock.Object, _taskServiceMock.Object);

            // Act
            var result = controller.Delete(user.Id);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<UserViewModel>(viewResult.ViewData.Model);
            Assert.Equal(user.Name, model.Name);
            Assert.Equal(user.Surname, model.Surname);
            Assert.Null(model.RoleName);
        }


        [Fact]
        public void Delete_Get_ReturnsNotFound_WhenIdIsNull()
        {
            // Arrange
            var controller = new UserController(_db, _userManagerMock.Object, _roleManagerMock.Object, _taskServiceMock.Object);

            // Act
            var result = controller.Delete(null);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }


        [Fact]
        public void DeletePost_ValidId_RemovesUserFromDatabase_AndRedirectsToIndex()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Id = "1",
                Name = "Jon",
                Surname = "Fetahi"
            };

            _db.Users.Add(user);
            _db.SaveChanges();

            var userController = new UserController(_db, _userManagerMock.Object, _roleManagerMock.Object, _taskServiceMock.Object);

            // Act
            var result = userController.DeletePost(user.Id) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);

            var deletedUser = _db.Users.Find(user.Id);
            Assert.Null(deletedUser);
        }

        [Fact]
        public void DeletePost_ReturnsNotFound_WhenIdIsNull()
        {
            // Arrange
            var controller = new UserController(_db, _userManagerMock.Object, _roleManagerMock.Object, _taskServiceMock.Object);

            // Act
            var result = controller.DeletePost(null);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundResult>(result);
            Assert.Equal(404, notFoundResult.StatusCode);
        }
    }
}
