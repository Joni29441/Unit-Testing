using System;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using ProjectManagementSystem.Controllers;
using ProjectManagementSystem.Data;
using ProjectManagementSystem.Models;
using ProjectManagementSystem.Models.DomainModels;
using ProjectManagementSystem.Models.ViewModels.AccountViewModels;
using ProjectManagementSystem.Services;

namespace ProjectManagementSystem.UnitTests.ControllerTest
{
    public class TaskControllerTest
    {
        private readonly TaskController _controller;
        private readonly ApplicationDbContext _context;
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<ITaskService> _taskServiceMock;

        public TaskControllerTest()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            _context = new ApplicationDbContext(options);
            _context.Database.EnsureCreated();

            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);

            _taskServiceMock = new Mock<ITaskService>();

            _controller = new TaskController(_taskServiceMock.Object, _context, _userManagerMock.Object);
        }

        [Fact]
        public void Index_ReturnsAViewResult_WithAListOfTaskViewModels()
        {
            // Arrange
            var tasks = new List<ProjectTask>
            {
                new ProjectTask
                {
                    Id = 1,
                    Name = "Task 1",
                    Status = status.New,
                    Deadline = DateTime.Now.AddDays(1),
                    Description = "Task 1 Description",
                    Progress = 0,
                    DeveloperId = null,
                    ManagerId = null,
                    IsDeveloperAssigned = false,
                    IsManagerAssigned = false,
                    AdminId = null,
                    ProjectId = 1
                },
                new ProjectTask
                {
                    Id = 2,
                    Name = "Task 2",
                    Status = status.InProgress,
                    Deadline = DateTime.Now.AddDays(2),
                    Description = "Task 2 Description",
                    Progress = 50,
                    DeveloperId = null,
                    ManagerId = null,
                    IsDeveloperAssigned = false,
                    IsManagerAssigned = false,
                    AdminId = null,
                    ProjectId = 1
                }
            };
            _context.Tasks.AddRange(tasks);
            _context.SaveChanges();

            // Act
            var result = _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<TaskViewModel>>(
                viewResult.ViewData.Model);
            Assert.Equal(2, model.Count());
        }


    }
}
