
using BulkyBook.DataAccess.Repository;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.DataAccess.Data;
using Microsoft.AspNetCore.Mvc;
using System.Reflection.Metadata;
using Microsoft.AspNetCore.Mvc.Rendering;
using BulkyBook.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Bulky.Utility;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles =SD.Role_Admin)]
    public class UserController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public UserController(ApplicationDbContext db, UserManager<IdentityUser> userManager, IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {
            //List<Company> objCompanyList = _unitOfWork.Company.GetAll().ToList();

            return View();
        }

        public IActionResult RoleManagement(string userId)
        {
            RoleManagementVM roleManagementVM = new RoleManagementVM()
            {
                ApplicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId, includeProperties:"Company"),
                RoleList = _roleManager.Roles.Select(u => new SelectListItem
                {
                    Value = u.Name,
                    Text = u.Name,
                }),
                CompanyList = _unitOfWork.Company.GetAll().Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                })

            };
            roleManagementVM.ApplicationUser.Role = _userManager.GetRolesAsync(_unitOfWork.ApplicationUser.Get(u => u.Id == userId)).GetAwaiter().GetResult().FirstOrDefault();
            return View(roleManagementVM);
        }

        [HttpPost]
        public IActionResult RoleManagement(RoleManagementVM roleManagementVM)
        {
            string oldRole = _userManager.GetRolesAsync(_unitOfWork.ApplicationUser.Get(u => u.Id == roleManagementVM.ApplicationUser.Id)).GetAwaiter().GetResult().FirstOrDefault(); ;
            ApplicationUser applicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == roleManagementVM.ApplicationUser.Id);


            if (!(roleManagementVM.ApplicationUser.Role == oldRole))
            {
                //update role

                if (roleManagementVM.ApplicationUser.Role == SD.Role_Company) {
                    applicationUser.CompanyId = roleManagementVM.ApplicationUser.CompanyId;
                }

                if(oldRole == SD.Role_Company)
                {
                    applicationUser.CompanyId = null;
                }
                _unitOfWork.ApplicationUser.Update(applicationUser);
                _unitOfWork.Save();

                _userManager.RemoveFromRoleAsync(applicationUser, oldRole).GetAwaiter().GetResult();
                _userManager.AddToRoleAsync(applicationUser, roleManagementVM.ApplicationUser.Role).GetAwaiter().GetResult();
            }
            else if(roleManagementVM.ApplicationUser.Role == SD.Role_Company)
            {
                applicationUser.CompanyId = roleManagementVM.ApplicationUser.CompanyId;
                _unitOfWork.ApplicationUser.Update(applicationUser);
                _unitOfWork.Save();
            }
            return View(nameof(Index));
        }


        //call api trong mvc
        #region API CALLS
        [HttpGet]
        public ActionResult GetAll()
        {
            
            List<ApplicationUser> objUserList = _unitOfWork.ApplicationUser.GetAll(includeProperties:"Company").ToList();
            //identity tiêm vào thì tự động vứt ASPNET trong database là có thể truy cập các table chứ không cần khai báo trong db
            //vì đây không có trong unitofwork nên cần phải tiêm trực tiếp _db vào để có thể sử dụng được
            //var userRoles = _db.UserRoles.ToList();
            //var roles = _db.Roles.ToList();
            //mặc dù chạy được company.name nhưng json ném ra lỗi vì một số user không có company.name trong js ajax
            foreach (var user in objUserList)
            {
                //xem đến video thứ 3 mai xem video 4
                //var roleID = userRoles.FirstOrDefault(u =>u.UserId == user.Id).RoleId;
                user.Role = _userManager.GetRolesAsync(user).GetAwaiter().GetResult().FirstOrDefault();

                if (user.Company == null)
                {
                    user.Company = new Company()
                    {
                        Name = ""
                    };
                }
            }
            return Json(new
            {
                data= objUserList
            });
        }
        [HttpPost]
        public IActionResult LockUnlock([FromBody]string id)
        {
            var objFromDb = _unitOfWork.ApplicationUser.Get(u => u.Id == id);
            if (objFromDb == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Error while Locking/Unlockin"
                });
            }
            if (objFromDb.LockoutEnd != null && objFromDb.LockoutEnd > DateTime.Now)
            {
                //user is locked and we need to unlock them
                objFromDb.LockoutEnd = DateTime.Now;
            }
            else
            {
                //lock 1000 years
                objFromDb.LockoutEnd = DateTime.Now.AddYears(1000);
            }
            _unitOfWork.ApplicationUser.Update(objFromDb);
            _unitOfWork.Save();
            
            return Json(new
            {
                success = true,
                message = "Successful"
            });
        }
        #endregion


    }
}
