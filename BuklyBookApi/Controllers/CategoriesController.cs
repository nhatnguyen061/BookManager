using Bulky.Utility;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BuklyBookApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        public CategoriesController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            List<Category> objCompanyList = _unitOfWork.Category.GetAll().ToList();
            return Ok(new { data = objCompanyList });
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var categoryFromDb = _unitOfWork.Category.Get(u => u.Id == id);
            if (categoryFromDb == null)
            {
                return NotFound(new { success = false, message = "Error: Category not found." });
            }
            _unitOfWork.Category.Delete(categoryFromDb);
            _unitOfWork.Save();
            return Ok(new { success = true, message = "Delete Successful" });
        }
    }
}
