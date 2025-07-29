using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace BuklyBookApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IWebHostEnvironment _webHostEnvironment;

        private readonly IUnitOfWork _unitOfWork;
        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }
        [HttpGet]
        public IActionResult GetAll()
        {
            List<Product> objCompanyList = _unitOfWork.Product.GetAll().ToList();
            return Ok(new { data = objCompanyList });
        }

        //[HttpDelete("{id}")]
        //public IActionResult Delete(int id)
        //{
        //    var productToBeDelete = _unitOfWork.Product.Get(u => u.Id == id);
        //    if (productToBeDelete == null)
        //    {
        //        return Json(new
        //        {
        //            success = false,
        //            message = "Error while deleting"
        //        });
        //    }

        //    string productPath = @"images\products\product-" + id;
        //    string finalPath = Path.Combine(_webHostEnvironment.WebRootPath, productPath);

        //    if (!Directory.Exists(finalPath))
        //    {
        //        //trước khi xóa tệp thì phải xóa các file trong tệp đó đi
        //        string[] filepaths = Directory.GetFiles(finalPath);
        //        foreach (string filepath in filepaths)
        //        {
        //            System.IO.File.Delete(filepath);
        //        }

        //        Directory.Delete(finalPath);
        //    }

        //    _unitOfWork.Product.Delete(productToBeDelete);
        //    _unitOfWork.Save();
        //    await _hubContext.Clients.All.SendAsync("ReceiveMessage");
        //    return Json(new
        //    {
        //        success = true,
        //        message = "Delete Successful"
        //    });
        //}
    }
}
