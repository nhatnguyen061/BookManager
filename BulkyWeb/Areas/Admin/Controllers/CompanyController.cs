
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

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles =SD.Role_Admin)]
    public class CompanyController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        public CompanyController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {
            //List<Company> objCompanyList = _unitOfWork.Company.GetAll().ToList();

            return View();
        }

        public IActionResult Upsert(int? id)
        {
            if (id == null || id == 0)
            {
                //create
                return View(new Company());
            }
            else
            {
                //update
                Company companyObj = _unitOfWork.Company.Get(u => u.Id == id);
                return View(companyObj);
            }
        }
        [HttpPost]
        public IActionResult Upsert(Company CompanyObj)
        {
            //check xem nếu obj truyền vào không hợp lệ với model khởi tạo
            if (ModelState.IsValid)
            {                
                
                if(CompanyObj.Id ==0)
                {
                    _unitOfWork.Company.Add(CompanyObj);
                }
                else
                {
                    _unitOfWork.Company.Update(CompanyObj);
                }
               
                _unitOfWork.Save();
                //tạo data tạm thời khi chuyển sang page khác

                TempData["success"] = "Company created successfully";
                //redirect đến action
                return RedirectToAction("Index");
            }
            else
            {               
                return View(CompanyObj);

            }

        }
      
        //public IActionResult Delete(int? id)
        //{
        //    if (id == null || id == 0)
        //    {
        //        return NotFound();
        //    }
        //    Company? CompanyFromDb = _unitOfWork.Company.Get(u => u.Id == id);
        //    if (CompanyFromDb == null)
        //    {
        //        return NotFound();
        //    }
        //    return View(CompanyFromDb);
        //}
        //[HttpPost, ActionName("Delete")]
        //public IActionResult DeletePOST(int? id)
        //{
        //    Company? obj = _unitOfWork.Company.Get(u => u.Id == id);
        //    if (obj == null)
        //    {
        //        return NotFound();
        //    }
        //    _unitOfWork.Company.Delete(obj);
        //    _unitOfWork.Save();
        //    TempData["success"] = "Company delete successfully";

        //    return RedirectToAction("Index");
        //}
        //call api trong mvc
        #region API CALLS
        [HttpGet]
        public IActionResult GetAll()
        {
            List<Company> objCompanyList = _unitOfWork.Company.GetAll().ToList();
            return Json(new
            {
                data=objCompanyList
            });
        }
        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var CompanyToBeDelete = _unitOfWork.Company.Get(u => u.Id == id);
            if(CompanyToBeDelete == null)
            {
                return Json(new
                {
                    success =false,
                    message = "Error while deleting"
                });
            }           

            _unitOfWork.Company.Delete(CompanyToBeDelete);
            _unitOfWork.Save();
            return Json(new
            {
                success = true,
                message = "Delete Successful"
            });
        }
        #endregion


    }
}
