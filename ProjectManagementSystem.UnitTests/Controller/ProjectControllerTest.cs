using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using ProjectManagementSystem.Controllers;
using ProjectManagementSystem.Data;
using ProjectManagementSystem.Models.DomainModels;
using ProjectManagementSystem.Services;
using System.Security.Claims;

namespace ProjectManagementSystem.UnitTests.Controller
{
    public class ProjectControllerTest
    {

        private readonly ApplicationDbContext _db;
        private readonly Mock<ITaskService> _taskServiceMock;
        private readonly ProjectController _controller;

        private ApplicationDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Use a unique database name for each test
                .Options;
            var dbContext = new ApplicationDbContext(options);
            dbContext.Database.EnsureCreated();
            return dbContext;
        }

        public ProjectControllerTest()
        {
            _taskServiceMock = new Mock<ITaskService>();
        }

        private ProjectController CreateController(ApplicationDbContext dbContext)
        {
            var controller = new ProjectController(dbContext, _taskServiceMock.Object);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "user1"),
                new Claim(ClaimTypes.Role, "Project Manager")
            }));

            var httpContext = new DefaultHttpContext
            {
                User = user
            };

            httpContext.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "managerId", "manager1" }
            });

            var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
            {
                ["ErrorMessage"] = "Please select a Project Manager."
            };

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            controller.TempData = tempData;

            return controller;
        }

        [Fact]
        public async void Create_InsertProjectAndRedirectsToIndex()
        {
            // Arrange
            var _db = CreateDbContext();
            var project = new Project { Id = 1, Name = "Test Project", Progress = 30, ProjectManagerId = "13dsd1213" };
            var controller = new ProjectController(_db, _taskServiceMock.Object);

            // Set up HTTP context with user and form data
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "13dsd1213"),
                new Claim(ClaimTypes.Role, "Project Manager")
            }));

            var httpContext = new DefaultHttpContext
            {
                User = user
            };

            httpContext.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "managerId", "manager1" }
            });

            var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
            {
                ["ErrorMessage"] = "Please select a Project Manager."
            };

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            controller.TempData = tempData;

            // Act
            var result = await controller.Create(project);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);

            var dbProject = await _db.Projects.FindAsync(1);
            Assert.NotNull(dbProject);
            Assert.Equal("Test Project", dbProject.Name);
            Assert.Equal("13dsd1213", dbProject.ProjectManagerId);
            Assert.Equal(30, dbProject.Progress);

            var finalCount = await _db.Projects.CountAsync();
            Assert.Equal(1, finalCount);
        }

        [Fact]
        public async Task CreatePost_AddsProjectAndRedirectsToIndex()
        {
            // Arrange
            var dbContext = CreateDbContext();
            var newProject = new Project { Id = 1, Name = "New Project", ProjectManagerId = "user1" };
            var controller = CreateController(dbContext);

            // Act
            var result = await controller.Create(newProject);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);

            var dbProject = await dbContext.Projects.FindAsync(1);
            Assert.NotNull(dbProject);
            Assert.Equal("New Project", dbProject.Name);
            Assert.Equal("user1", dbProject.ProjectManagerId);
            Assert.Equal(0, dbProject.Progress);
        }

        [Fact]
        public void EditPost_UpdatesProjectAndRedirectToIndex()
        {
            // Arrange
            var dbContext = CreateDbContext();
            var project = new Project { Id = 1, Name = "Original Project", ProjectManagerId = "12" };
            dbContext.Projects.Add(project);
            dbContext.SaveChanges();

            var updatedProject = new Project { Id = 1, Name = "Updated Project", ProjectManagerId = "12" };
            var controller = CreateController(dbContext);

            // Act
            var result = controller.Edit(updatedProject);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            var dbProject = dbContext.Projects.Find(1);
            Assert.NotNull(dbProject);
            Assert.Equal("Updated Project", dbProject.Name);
            Assert.Equal("12", dbProject.ProjectManagerId);
        }

        [Fact]
        public void EditPost_UpdateProjectManagerId()
        {
            // Arrange
            var dbContext = CreateDbContext();
            var project = new Project { Id = 1, Name = "Original Project", ProjectManagerId = "12" };
            dbContext.Projects.Add(project);
            dbContext.SaveChanges();

            var updatedProject = new Project { Id = 1, Name = "Original Project", ProjectManagerId = "13" };
            var controller = CreateController(dbContext);

            // Act
            var result = controller.Edit(updatedProject);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            var dbProject = dbContext.Projects.Find(1);
            Assert.NotNull(dbProject);
            Assert.Equal("13", dbProject.ProjectManagerId);
        }


        [Fact]
        public void EditPost_ReturnsNotFound_WhenProjectDoesNotExist()
        {
            // Arrange
            var dbContext = CreateDbContext();
            var controller = CreateController(dbContext);
            var updatedProject = new Project { Id = 999, Name = "Updated Project", ProjectManagerId = "12" };

            // Act
            var result = controller.Edit(updatedProject);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundResult>(result);
            Assert.Equal(404, notFoundResult.StatusCode);
        }


        [Fact]
        public void EditPost_ReturnsViewWithErrors_WhenModelStateIsInvalid()
        {
            // Arrange
            var dbContext = CreateDbContext();
            var project = new Project { Id = 1, Name = "Some Project", ProjectManagerId = "12" };
            dbContext.Projects.Add(project);
            dbContext.SaveChanges();

            var controller = CreateController(dbContext);
            controller.ModelState.AddModelError("Name", "Required");

            var updatedProject = new Project { Id = 1, Name = "Updated Project", ProjectManagerId = "12" };

            // Act
            var result = controller.Edit(updatedProject);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Edit", viewResult.ViewName); 
            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey("Name"));
        }




        [Fact]
        public void DeletePost_ReturnsNotFound_WhenIdIsNull()
        {
            // Arrange
            var dbContext = CreateDbContext();
            var controller = CreateController(dbContext);

            // Act
            var result = controller.DeletePost(null);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundResult>(result);
            Assert.Equal(404, notFoundResult.StatusCode);
        }

        [Fact]
        public void DeletePost_RemovesProjectAndRedirectsToIndex_WhenProjectExists()
        {
            // Arrange
            var dbContext = CreateDbContext();
            var project = new Project { Id = 2, Name = "Test Project", Progress = 20, ProjectManagerId = "sad1231" };
            dbContext.Projects.Add(project);
            dbContext.SaveChanges();

            var controller = CreateController(dbContext);

            // Act
            var result = controller.DeletePost(project.Id);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            Assert.Null(redirectToActionResult.ControllerName);

            Assert.Equal(0, dbContext.Projects.Count());
        }

        [Fact]
        public void DeletePost_NonExistingProject_ReturnsNotFound()
        {
            // Arrange
            var dbContext = CreateDbContext();
            var controller = CreateController(dbContext);

            // Act
            var result = controller.DeletePost(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void DeletePost_ExistingProject_DeletesProjectAndRedirectsToIndex()
        {
            // Arrange
            var dbContext = CreateDbContext();
            var project = new Project { Id = 222, Name = "Test Project", ProjectManagerId = "TestManager" };
            dbContext.Projects.Add(project);
            dbContext.SaveChanges();

            var controller = CreateController(dbContext);

            // Act
            var result = controller.DeletePost(222);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Null(dbContext.Projects.Find(222));
        }

        [Fact]
        public void Get_ListOfProjects()
        {
            // Arrange
            var dbContext = CreateDbContext();
            dbContext.Projects.AddRange(new List<Project>
            {
                new Project { Id = 3, Name = "Project 3", ProjectManagerId = "Manager3" },
                new Project { Id = 4, Name = "Project 4", ProjectManagerId = "Manager4" }
            });
            dbContext.SaveChanges();

            var controller = CreateController(dbContext);

            // Act
            var result = controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Project>>(viewResult.ViewData.Model);
            Assert.Equal(2, model.Count());
        }
    }
}