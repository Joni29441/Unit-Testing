using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using ProjectManagementSystem.Controllers;
using ProjectManagementSystem.Data;
using ProjectManagementSystem.Models.DomainModels;
using ProjectManagementSystem.Models.ViewModels;
using ProjectManagementSystem.Services;

namespace ProjectManagementSystemUnitTests.ControllerTests
{
    public class ProjectControllerTests
    {

        private readonly ApplicationDbContext _db;
        private readonly Mock<ITaskService> _taskServiceMock;
        private readonly ProjectController _controller;

        public ProjectControllerTests()
        {
            // Setup test database
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
            .Options;
            _db = new ApplicationDbContext(options);
            _db.Database.EnsureCreated();

            // Setup task service mock
            _taskServiceMock = new Mock<ITaskService>();
        }

        [Fact]
        public async void Create_InsertProjectAndRedirectsToIndex()
        {
            //Arrange
            var project = new Project { Id = 2, Name = "Test Project", Progress = 30, ProjectManagerId = "13dsd1213" };

            //Act
            var result = _db.Projects.Add(project);

            //Assert
            var initialCount = _db.Projects.Count();
            await _db.SaveChangesAsync();
            var finalCount = _db.Projects.Count();
            Assert.Equal(initialCount + 1, finalCount);
        }

        [Fact]
        public async void EditPost_UpdatesProjectAndRedirectToIndex()
        {
            //Arrange
            var project = new Project { Id = 1, Name = "Original Project", ProjectManagerId = "123456abc" };
            _db.Projects.Add(project);
            _db.SaveChangesAsync();

            var updatedProject = new Project { Id = 1, Name = "Updated Project", ProjectManagerId = "123456abc" };
            var formCollection = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());

            var httpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
                  {
                    new Claim(ClaimTypes.NameIdentifier, "user1")
                  }))
            };
            
           
            // Act
            var result = _controller.Edit(updatedProject);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            var dbProject = await _db.Projects.FindAsync(1);
            Assert.NotNull(dbProject);
            Assert.Equal("Updated Project", dbProject.Name);
            Assert.Null(dbProject.ProjectManagerId);
        }

        [Fact]
        public void DeletePost_ReturnsNotFound_WhenIdIsNull()
        {
            // Arrange
            var controller = new ProjectController(_db, _taskServiceMock.Object);

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
            var project = new Project { Id = 1, Name = "Test Project", Progress = 20, ProjectManagerId = "sad1231" };
            _db.Projects.Add(project);
            _db.SaveChanges();

            var controller = new ProjectController(_db, _taskServiceMock.Object);

            // Act
            var result = controller.DeletePost(project.Id);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            Assert.Null(redirectToActionResult.ControllerName);

            Assert.Equal(0, _db.Projects.Count());
        }

        [Fact]
        public void DeletePost_NonExistingProject_ReturnsNotFound()
        {
            //Arrange
            var controller = new ProjectController(_db, _taskServiceMock.Object);

            // Act
            var result = controller.DeletePost(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void DeletePost_ExistingProject_DeletesProjectAndRedirectsToIndex()
        {
            // Arrange
            var project = new Project { Id = 1, Name = "Test Project", ProjectManagerId = "TestManager" };
            _db.Projects.Add(project);
            _db.SaveChanges();

            var controller = new ProjectController(_db, _taskServiceMock.Object);

            // Act
            var result = controller.DeletePost(1);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Null(_db.Projects.Find(1));
        }



    }

}