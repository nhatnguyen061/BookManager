
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
using BulkyBookWeb.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        //link đến wwwroot để lấy đường dẫn cho file tĩnh
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IHubContext<BookHub> _hubContext;
        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment, IHubContext<BookHub> hubContext)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
            _hubContext = hubContext;
        }
        public IActionResult Index()
        {
            //List<Product> objProductList = _unitOfWork.Product.GetAll(includeProperties:"Category").ToList();

            return View();
        }

        public IActionResult Upsert(int? id)
        {
            //chuyển dữ liệu thành loại selectlist như trong html
            IEnumerable<SelectListItem> CategoryList = _unitOfWork.Category
                                                        .GetAll().Select(u => new SelectListItem
                                                        {
                                                            Text = u.Name,
                                                            Value = u.Id.ToString()
                                                        });
            //ViewBag.CategoryList = CategoryList;
            ProductVM productVM = new ProductVM
            {
                CategoryList = CategoryList,
                Product = new Product()
            };
            if (id == null || id == 0)
            {
                //create
                return View(productVM);
            }
            else
            {
                //update
                productVM.Product = _unitOfWork.Product.Get(u => u.Id == id, includeProperties: "ProductImages");
                return View(productVM);
            }
        }
        [HttpPost]
        public async Task<IActionResult> Upsert(ProductVM productVM, List<IFormFile>? files)
        {
            //check xem nếu obj truyền vào không hợp lệ với model khởi tạo
            if (ModelState.IsValid)
            {
                if (productVM.Product.Id == 0)
                {
                    _unitOfWork.Product.Add(productVM.Product);
                }
                else
                {
                    _unitOfWork.Product.Update(productVM.Product);
                }

                _unitOfWork.Save();

                string wwwRootPath = _webHostEnvironment.WebRootPath;
                if (files != null)
                {
                    foreach (IFormFile file in files)
                    {
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        string productPath = @"images\products\product-" + productVM.Product.Id;
                        string finalPath = Path.Combine(wwwRootPath, productPath);

                        if (!Directory.Exists(finalPath))
                        {
                            Directory.CreateDirectory(finalPath);
                        }

                        using (var fileStream = new FileStream(Path.Combine(finalPath, fileName), FileMode.Create))
                        {
                            file.CopyTo(fileStream);
                        }

                        ProductImage productImage = new ProductImage()
                        {
                            ImageUrl = @"\" + productPath + @"\" + fileName,
                            ProductId = productVM.Product.Id,
                        };

                        if (productVM.Product.ProductImages == null)
                        {
                            productVM.Product.ProductImages = new List<ProductImage>();
                        }
                        productVM.Product.ProductImages.Add(productImage);
                        _unitOfWork.ProductImage.Add(productImage);
                    }
                    _unitOfWork.Product.Update(productVM.Product);
                    _unitOfWork.Save();
                }

                //tạo data tạm thời khi chuyển sang page khác
                await _hubContext.Clients.All.SendAsync("ReceiveMessage");
                TempData["success"] = "Product created/updated successfully";
                //redirect đến action
                return RedirectToAction("Index");
            }
            else
            {
                IEnumerable<SelectListItem> CategoryList = _unitOfWork.Category
                                                        .GetAll().Select(u => new SelectListItem
                                                        {
                                                            Text = u.Name,
                                                            Value = u.Id.ToString()
                                                        });
                productVM.CategoryList = CategoryList;
                return View(productVM);

            }

        }

        public IActionResult DeleteImage(int imageId)
        {
            var imageToBeDelete = _unitOfWork.ProductImage.Get(u => u.Id == imageId);
            var productId = imageToBeDelete.ProductId;
            if (imageToBeDelete != null)
            {
                if (!string.IsNullOrEmpty(imageToBeDelete.ImageUrl))
                {
                    var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath,
                                       imageToBeDelete.ImageUrl.TrimStart('\\'));

                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }
                _unitOfWork.ProductImage.Delete(imageToBeDelete);
                _unitOfWork.Save();
                TempData["success"] = "Delete Image successfully";

            }
            return RedirectToAction(nameof(Upsert), new
            {
                id = productId
            });

        }


        //public IActionResult Delete(int? id)
        //{
        //    if (id == null || id == 0)
        //    {
        //        return NotFound();
        //    }
        //    Product? productFromDb = _unitOfWork.Product.Get(u => u.Id == id);
        //    if (productFromDb == null)
        //    {
        //        return NotFound();
        //    }
        //    return View(productFromDb);
        //}
        //[HttpPost, ActionName("Delete")]
        //public IActionResult DeletePOST(int? id)
        //{
        //    Product? obj = _unitOfWork.Product.Get(u => u.Id == id);
        //    if (obj == null)
        //    {
        //        return NotFound();
        //    }
        //    _unitOfWork.Product.Delete(obj);
        //    _unitOfWork.Save();
        //    TempData["success"] = "Product delete successfully";

        //    return RedirectToAction("Index");
        //}
        //call api trong mvc
        #region API CALLS
        [HttpGet]
        public IActionResult GetAll()
        {
            List<Product> objProductList = _unitOfWork.Product.GetAll(includeProperties: "Category").ToList();
            return Json(new
            {
                data = objProductList
            });
        }
        [HttpDelete]
        public async Task<IActionResult> Delete(int? id)
        {
            var productToBeDelete = _unitOfWork.Product.Get(u => u.Id == id);
            if (productToBeDelete == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Error while deleting"
                });
            }

            string productPath = @"images\products\product-" + id;
            string finalPath = Path.Combine(_webHostEnvironment.WebRootPath, productPath);

            if (!Directory.Exists(finalPath))
            {
                //trước khi xóa tệp thì phải xóa các file trong tệp đó đi
                string[] filepaths = Directory.GetFiles(finalPath);
                foreach (string filepath in filepaths)
                {
                    System.IO.File.Delete(filepath);
                }

                Directory.Delete(finalPath);
            }

            _unitOfWork.Product.Delete(productToBeDelete);
            _unitOfWork.Save();
            await _hubContext.Clients.All.SendAsync("ReceiveMessage");
            return Json(new
            {
                success = true,
                message = "Delete Successful"
            });
        }

        


        #endregion


    }
}

