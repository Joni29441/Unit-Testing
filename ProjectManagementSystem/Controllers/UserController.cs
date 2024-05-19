using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem.Data;
using ProjectManagementSystem.Models;
using ProjectManagementSystem.Models.ViewModels;
using ProjectManagementSystem.Services;


namespace ProjectManagementSystem.Controllers
{
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ITaskService _iTaskService;

        public UserController(ApplicationDbContext db, UserManager<ApplicationUser> userManager,
                              RoleManager<IdentityRole> roleManager, ITaskService iTaskService)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
            _iTaskService = iTaskService;
        }

        // GET:
        // Returning View with a List of Users
        public IActionResult Index()
        {
               
            List<UserViewModel> userList = _iTaskService.getUserList();

            return View(userList);
        }

        // GET:
        // EDIT VIEW
        public IActionResult Edit(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = _db.Users.Find(id);

            if (user == null)
            {
                return NotFound();
            }

            var roleId = (from ur in _db.UserRoles
                          where ur.UserId == user.Id
                          select ur.RoleId).FirstOrDefault();

            string roleName = null;

            if (roleId != null)
            {
                roleName = (from r in _db.Roles
                            where r.Id == roleId
                            select r.Name).FirstOrDefault();
            }

            UserViewModel userViewModel = new UserViewModel
            {
                Id = user.Id,
                Name = user.Name,
                Surname = user.Surname,
                RoleName = roleName
            };

            return View(userViewModel);
        }


        // POST:
        // Update Edited User
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserViewModel userViewModel)
        {
            var user =  await _userManager.FindByIdAsync(userViewModel.Id);

            if (user == null)
            {
                return NotFound();
            }

            string roleName = Request.Form["RoleName"];

            if (string.IsNullOrEmpty(roleName))
            {
                return BadRequest();
            }

            var role =  await _roleManager.FindByNameAsync(roleName);

            if (role == null)
            {
                return NotFound();
            }

            var userRoles = await _userManager.GetRolesAsync(user);

            await _userManager.RemoveFromRolesAsync(user, userRoles);
            await _userManager.AddToRoleAsync(user, roleName);
            await _userManager.UpdateAsync(user);

            return RedirectToAction("Index");
        }

        // GET:
        // Returning View for Deleting User
        public IActionResult Delete(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = _db.Users.Find(id);

            if (user == null)
            {
                return NotFound();
            }

            var roleId = (from ur in _db.UserRoles
                          where ur.UserId == user.Id
                          select ur.RoleId).FirstOrDefault();

            string roleName = null;

            if (roleId != null)
            {
                roleName = (from r in _db.Roles
                            where r.Id == roleId
                            select r.Name).FirstOrDefault();
            }

            UserViewModel userViewModel = new UserViewModel
            {
                Id = user.Id,
                Name = user.Name,
                Surname = user.Surname,
                RoleName = roleName
            };

            return View(userViewModel);
        }

        // POST:
        // Deleting User from Database by User's Id
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePost(string? id)
        {
            var user = _db.Users.Find(id);
            if (user == null)
            {
                return NotFound();
            }
            _db.Users.Remove(user);
            _db.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}


