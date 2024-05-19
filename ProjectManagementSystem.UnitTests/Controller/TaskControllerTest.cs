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
using Microsoft.AspNetCore.Mvc.Rendering;
using ProjectManagementSystem.Models.ViewModels;

namespace ProjectManagementSystem.UnitTests.Controller
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
                .UseInMemoryDatabase(databaseName: "TestDatabase" + Guid.NewGuid().ToString())
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

        [Fact]
        public void Create_Get_ReturnsViewResult_WithDeveloperAndProjectLists()
        {
            //Arrange
            _taskServiceMock.Setup(service => service.getDeveloperList()).Returns(new List<DeveloperViewModel>());
            _taskServiceMock.Setup(service => service.getProjectList()).Returns(new List<Project>());
            
            //Act
            var result = _controller.Create();

            //Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(viewResult.ViewData["DeveloperList"]);
            Assert.NotNull(viewResult.ViewData["ProjectList"]);
        }

        [Fact]
        public async Task Create_Post_InvalidModel_ReturnsView()
        {
            // Arrange
            var taskViewModel = new TaskViewModel
            {
                Name = "New Task",
                Description = "Description for New Task",
                Deadline = "19/19/2024", //Invalid Date
                DeveloperId = "1",
                ProjectId = 1
            };

            _controller.ModelState.AddModelError("Deadline", "19/19/2024");

            _taskServiceMock.Setup(service => service.getDeveloperList()).Returns(new List<DeveloperViewModel>());
            _taskServiceMock.Setup(service => service.getProjectList()).Returns(new List<Project>());

            // Act
            var result = await _controller.Create(taskViewModel);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(taskViewModel, viewResult.Model);
        }

        [Fact]
        public async Task Create_Post_ValidModel_RedirectsToIndex()
        {
            // Arrange
            var taskViewModel = new TaskViewModel
            {
                Name = "New Task",
                Description = "Description for New Task",
                Deadline = "01.01.2023",
                DeveloperId = "1",
                ProjectId = 1
            };

            var currentUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1")
            }, "mock"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = currentUser,
                    Request = { Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "developerId", taskViewModel.DeveloperId },
                { "projectId", taskViewModel.ProjectId.ToString() }
            }) }
                }
            };

            var project = new Project
            {
                Id = 1,
                Name = "Project 1",
                ProjectManagerId = "1"
            };

            var user = new ApplicationUser
            {
                Id = "1",
                Name = "John",
                Surname = "Doe"
            };

            _context.Projects.Add(project);
            _context.Users.Add(user);
            _context.SaveChanges();

            // Act
            var result = await _controller.Create(taskViewModel);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);

            var dbTask = _context.Tasks.FirstOrDefault(t => t.Name == "New Task");
            Assert.NotNull(dbTask);
            Assert.Equal("Description for New Task", dbTask.Description);
        }


       


        [Fact]
        public async Task Edit_ReturnsViewResult_WhenUserIsAdmin()
        {
            // Arrange
            var task = new ProjectTask
            {
                Id = 3,
                Name = "Task 3",
                Status = status.New,
                Deadline = DateTime.Now.AddDays(1),
                Description = "Description for Task 3",
                Progress = 50,
                DeveloperId = "1",
                ManagerId = "2",
                IsDeveloperAssigned = true,
                IsManagerAssigned = true,
                ProjectId = 1
            };
            _context.Tasks.Add(task);
            _context.SaveChanges();

            var taskViewModel = new TaskViewModel
            {
                Id = 3,
                Name = "Task 3 UPDATED",
                Status = status.New,
                Deadline = "12.04.2023",
                Description = "Description for Task 3 UPDATED",
                Progress = 50,
                DeveloperId = "1",
                ManagerId = "2",
                IsDeveloperAssigned = true,
                ProjectId = 1
            };

            var currentUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Name, "Joni"),
                new Claim(ClaimTypes.Role, "Admin"),
            }, "mock"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = currentUser
                }
            };

            // Act
            var result = _controller.Edit(taskViewModel.Id);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<TaskViewModel>(viewResult.ViewData.Model);
            Assert.Equal(task.Name, model.Name);
            Assert.Equal(task.Description, model.Description);
        }

        [Fact]
        public void EditGet_ReturnsNotFound_WhenId_IsInvalid()
        {
            // Act
            var result = _controller.Edit((int?)null);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void EditGet_ReturnsViewResult_WithTaskViewModel()
        {
            // Arrange
            var task = new ProjectTask
            {
                Id = 1,
                Name = "Task 1",
                Status = status.New,
                Deadline = DateTime.Now.AddDays(1),
                Description = "Description for Task 1",
                Progress = 0,
                DeveloperId = "1",
                ManagerId = "2",
                ProjectId = 1
            };
            _context.Tasks.Add(task);
            _context.SaveChanges();

            _taskServiceMock.Setup(service => service.getDeveloperList()).Returns(new List<DeveloperViewModel>());

            // Act
            var result = _controller.Edit(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<TaskViewModel>(viewResult.ViewData.Model);
            Assert.Equal(task.Name, model.Name);
        }




        [Fact]
        public void Edit_Post_ReturnsView_WhenModelStateIsInvalid()
        {
            // Arrange
            var taskViewModel = new TaskViewModel
            {
                Id = 1,
                Name = "Task 1",
                Status = status.New,
                Deadline = "Invalid Date",
                Description = "Description for Task 1",
                Progress = 50,
                DeveloperId = "1",
                ManagerId = "2",
                ProjectId = 1
            };

            _controller.ModelState.AddModelError("Deadline", "Invalid date format");

            _taskServiceMock.Setup(service => service.getDeveloperList()).Returns(new List<DeveloperViewModel>());
            _taskServiceMock.Setup(service => service.getProjectList()).Returns(new List<Project>());


            // Act
            var result = _controller.Edit(taskViewModel);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(taskViewModel, viewResult.Model);
        }

        [Fact]
        public void Delete_ValidTask_RedirectToAction()
        {
            // Arrange
            var task = new ProjectTask
            {
                Id = 2,
                Name = "Task 2",
                Status = status.New,
                Deadline = DateTime.Now.AddDays(1),
                Description = "Description for Task 2",
                Progress = 50,
                DeveloperId = "1",
                ManagerId = "2",
                IsDeveloperAssigned = true,
                IsManagerAssigned = true,
                ProjectId = 1
            };
            _context.Tasks.Add(task);
            _context.SaveChanges();

            var project = new Project
            {
                Id = 1,
                Name = "Project 1",
                Progress = 30,
                ProjectManagerId = "1"
            };
            _context.Projects.Add(project);
            _context.SaveChanges();

            var taskViewModel = new TaskViewModel
            {
                Id = 2,
                Name = "Task 2",
                Status = status.New,
                Deadline = "12.04.2023",
                Description = "Description for Task 2",
                Progress = 50,
                DeveloperId = "1",
                ManagerId = "2",
                IsDeveloperAssigned = true,
                ProjectId = 1
            };

            // Act
            var result = _controller.Delete(taskViewModel);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);

            var taskInDb = _context.Tasks.Find(taskViewModel.Id);
            Assert.Null(taskInDb);
        }


        [Fact]
        public void Delete_InvalidTask_ReturnNotFound()
        {
            // Act
            var result = _controller.Delete(new TaskViewModel { Id = 100 });

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void Delete_Get_ReturnsNotFound_WhenIdIsInvalid()
        {
            // Act
            var result = _controller.Delete((int?)null);
            // Assert
            Assert.IsType<NotFoundResult>(result);
        }


        [Fact]
        public void Delete_Get_ReturnsViewResult_WithTaskViewModel()
        {
            // Arrange
            var task = new ProjectTask
            {
                Id = 1,
                Name = "Task 1",
                Status = status.New,
                Deadline = DateTime.Now.AddDays(1),
                Description = "Description for Task 1",
                Progress = 0,
                DeveloperId = "1",
                ManagerId = "2",
                ProjectId = 1
            };
            _context.Tasks.Add(task);
            _context.SaveChanges();

            var project = new Project
            {
                Id = 1,
                Name = "Project 1",
                Progress = 30,
                ProjectManagerId = "1"
            };
            _context.Projects.Add(project);
            _context.SaveChanges();

            var developer = new ApplicationUser
            {
                Id = "1",
                Name = "Developer",
                Surname = "One"
            };
            _context.Users.Add(developer);
            _context.SaveChanges();

            // Act
            var result = _controller.Delete(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<TaskViewModel>(viewResult.ViewData.Model);
            Assert.Equal(task.Name, model.Name);
        }


        [Fact]
        public void Delete_Post_ValidTask_RedirectToAction()
        {
            // Arrange
            var task = new ProjectTask
            {
                Id = 2,
                Name = "Task 2",
                Status = status.New,
                Deadline = DateTime.Now.AddDays(1),
                Description = "Description for Task 2",
                Progress = 50,
                DeveloperId = "1",
                ManagerId = "2",
                IsDeveloperAssigned = true,
                IsManagerAssigned = true,
                ProjectId = 1
            };
            _context.Tasks.Add(task);
            _context.SaveChanges();

            var project = new Project
            {
                Id = 1,
                Name = "Project 1",
                Progress = 30,
                ProjectManagerId = "1"
            };
            _context.Projects.Add(project);
            _context.SaveChanges();

            var taskViewModel = new TaskViewModel
            {
                Id = 2,
                Name = "Task 2",
                Status = status.New,
                Deadline = "12.04.2023",
                Description = "Description for Task 2",
                Progress = 50,
                DeveloperId = "1",
                ManagerId = "2",
                IsDeveloperAssigned = true,
                ProjectId = 1
            };

            // Act
            var result = _controller.Delete(taskViewModel);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);

            var taskInDb = _context.Tasks.Find(taskViewModel.Id);
            Assert.Null(taskInDb);
        }

        [Fact]
        public void Delete_Post_ReturnsNotFound_WhenTaskIsNull()
        {
            // Arrange
            var taskViewModel = new TaskViewModel
            {
                Id = 100, // Invalid Id
                Name = "Task 100"
            };

            // Act
            var result = _controller.Delete(taskViewModel);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }


    }
}
