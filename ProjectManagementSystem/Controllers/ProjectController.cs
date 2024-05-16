using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ProjectManagementSystem.Data;
using ProjectManagementSystem.Models.DomainModels;
using ProjectManagementSystem.Models.ViewModels;
using ProjectManagementSystem.Services;


namespace ProjectManagementSystem.Controllers
{
    public class ProjectController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ITaskService _iTaskService;

        public ProjectController(ApplicationDbContext db, ITaskService taskService)
        {
            _db = db;
            _iTaskService = taskService;
        }

        // GET: 
        // Returning View with List of Projects       
        public IActionResult Index()
        {
            IEnumerable<Project> projectList = _db.Projects;
            return View(projectList);
        }

        // GET: 
        // Returning View for Creation of Project with List of Managers
        public IActionResult Create()
        {
            ViewBag.ManagerList = _iTaskService.getManagerList();
            return View();
        }

        // POST:
        // Creating New Project and adding it to database
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Project newProject)
        {

            if (newProject == null)
            {
                return BadRequest();
            }

            var currentUser = HttpContext.User;
            if (currentUser == null)
            {
                return Forbid();
            }

            var roleName = currentUser.FindFirst(ClaimTypes.Role)?.Value;
            var currentUserId = currentUser.FindFirstValue(ClaimTypes.NameIdentifier);
            var managerId = HttpContext.Request.Form["managerId"];

            if (roleName == "Project Manager")
            {
                managerId = currentUserId;
            }

            if (string.IsNullOrEmpty(managerId))
            {
                TempData["ErrorMessage"] = "Please select a Project Manager.";
                ViewBag.ManagerList = _iTaskService.getManagerList();
                return View("Create");
            }

            newProject.ProjectManagerId = managerId;
            // Ensure Progress is not modified
            _db.Add(newProject);
            await _db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        // GET:
        // Returning View for Editing of an existing Project
        public IActionResult Edit(int? id)
        {
            if (id == 0 || id == null)
            {
                return NotFound();
            }
            var project = _db.Projects.Find(id);
            if (project == null)
            {
                return NotFound();
            }

            ViewBag.ManagerList = _iTaskService.getManagerList();
            ViewBag.ProjectManagerId = project.ProjectManagerId;
            return View(project);
        }

        // POST:
        // Updating Existing Project and Saving to Database
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Project project)
        {
            if (project == null || !_db.Projects.Any(p => p.Id == project.Id))
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View("Edit", project);
            }

            var dbProject = _db.Projects.Find(project.Id);
            if (dbProject == null)
            {
                return NotFound();
            }
            dbProject.Name = project.Name;
            dbProject.ProjectManagerId = project.ProjectManagerId;

            _db.Update(dbProject);
            _db.SaveChanges();

            return RedirectToAction("Index");
        }

        // GET:
        // Returning View for Deleting a Project
        public IActionResult Delete(int? id)
        {
            if (id == 0 || id == null)
            {
                return NotFound();
            }
            var project = _db.Projects.Find(id);
            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        // POST:
        // Deleting the Project from Database by Project's Id
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePost(int? id)
        {
            var project = _db.Projects.Find(id);
            if (project == null)
            {
                return NotFound();
            }
            _db.Projects.Remove(project);
            _db.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}

