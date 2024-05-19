using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem.Data;
using ProjectManagementSystem.Models;
using ProjectManagementSystem.Models.DomainModels;
using ProjectManagementSystem.Models.ViewModels;
using ProjectManagementSystem.Services;
using ProjectManagementSystem.Helper;
using Xunit;
using Microsoft.AspNetCore.Identity;

namespace ProjectManagementSystemUnitTests.ServiceTests
{
    public class TaskServiceImplTests
    {
        private readonly TestApplicationDbContext _dbContext;
        private readonly TaskServiceImpl _taskService;

        public TaskServiceImplTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase" + Guid.NewGuid().ToString())
                .Options;
            _dbContext = new TestApplicationDbContext(options);
            _taskService = new TaskServiceImpl(_dbContext);
        }

        [Fact]
        public void getDeveloperList_ReturnsListOfDevelopers()
        {
            // Arrange
            var users = new List<ApplicationUser>
            {
                new ApplicationUser { Id = "1", Name = "John", Surname = "Doe" },
                new ApplicationUser { Id = "2", Name = "Jane", Surname = "Smith" }
            }.AsQueryable();

            var userRoles = new List<IdentityUserRole<string>>
            {
                new IdentityUserRole<string> { UserId = "1", RoleId = "1" },
                new IdentityUserRole<string> { UserId = "2", RoleId = "1" }
            }.AsQueryable();

            var roles = new List<IdentityRole>
            {
                new IdentityRole { Id = "1", Name = Helper.Developer }
            }.AsQueryable();

            _dbContext.Users.AddRange(users);
            _dbContext.UserRoles.AddRange(userRoles);
            _dbContext.Roles.AddRange(roles);
            _dbContext.SaveChanges();

            // Act
            var result = _taskService.getDeveloperList();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void getManagerList_ReturnsListOfManagers()
        {
            // Arrange
            var users = new List<ApplicationUser>
            {
                new ApplicationUser { Id = "1", Name = "John", Surname = "Doe" },
                new ApplicationUser { Id = "2", Name = "Jane", Surname = "Smith" }
            }.AsQueryable();

            var userRoles = new List<IdentityUserRole<string>>
            {
                new IdentityUserRole<string> { UserId = "1", RoleId = "1" },
                new IdentityUserRole<string> { UserId = "2", RoleId = "1" }
            }.AsQueryable();

            var roles = new List<IdentityRole>
            {
                new IdentityRole { Id = "1", Name = Helper.ProjectManager }
            }.AsQueryable();

            _dbContext.Users.AddRange(users);
            _dbContext.UserRoles.AddRange(userRoles);
            _dbContext.Roles.AddRange(roles);
            _dbContext.SaveChanges();

            // Act
            var result = _taskService.getManagerList();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void getUserList_ReturnsListOfUsers()
        {
            // Arrange
            var users = new List<ApplicationUser>
            {
                new ApplicationUser { Id = "1", Name = "John", Surname = "Doe" },
                new ApplicationUser { Id = "2", Name = "Jane", Surname = "Smith" }
            }.AsQueryable();

            var userRoles = new List<IdentityUserRole<string>>
            {
                new IdentityUserRole<string> { UserId = "1", RoleId = "1" },
                new IdentityUserRole<string> { UserId = "2", RoleId = "2" }
            }.AsQueryable();

            var roles = new List<IdentityRole>
            {
                new IdentityRole { Id = "1", Name = Helper.Developer },
                new IdentityRole { Id = "2", Name = Helper.ProjectManager }
            }.AsQueryable();

            _dbContext.Users.AddRange(users);
            _dbContext.UserRoles.AddRange(userRoles);
            _dbContext.Roles.AddRange(roles);
            _dbContext.SaveChanges();

            // Act
            var result = _taskService.getUserList();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void getProjectList_ReturnsListOfProjects()
        {
            // Arrange
            var projects = new List<Project>
            {
                new Project { Id = 1, Name = "Project 1", ProjectManagerId = "P1" },
                new Project { Id = 2, Name = "Project 2", ProjectManagerId = "P2" }
            }.AsQueryable();

            _dbContext.Projects.AddRange(projects);
            _dbContext.SaveChanges();

            // Act
            var result = _taskService.getProjectList();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void getStatusList_ReturnsListOfStatuses()
        {
            // Act
            var result = _taskService.getStatusList();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Contains(Helper.New, result);
            Assert.Contains(Helper.InProgress, result);
            Assert.Contains(Helper.Finished, result);
        }

         [Fact]
        public void getDeveloperList_ReturnsEmptyList_WhenNoDevelopersExist()
        {
            // Arrange
            // No developers in the database

            // Act
            var result = _taskService.getDeveloperList();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void getManagerList_ReturnsEmptyList_WhenNoManagersExist()
        {
            // Arrange
            // No managers in the database

            // Act
            var result = _taskService.getManagerList();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void getUserList_ReturnsEmptyList_WhenNoUsersExist()
        {
            // Arrange
            // No users in the database

            // Act
            var result = _taskService.getUserList();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void getProjectList_ReturnsEmptyList_WhenNoProjectsExist()
        {
            // Arrange
            // No projects in the database

            // Act
            var result = _taskService.getProjectList();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void getStatusList_ReturnsCorrectStatuses()
        {
            // Act
            var result = _taskService.getStatusList();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Equal(new List<string> { Helper.New, Helper.InProgress, Helper.Finished }, result);
        }
    }
    

    public class TestApplicationDbContext : ApplicationDbContext
    {
        public TestApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
    }
}
