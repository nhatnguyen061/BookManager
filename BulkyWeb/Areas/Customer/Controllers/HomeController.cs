using Bulky.Utility;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Claims;

namespace BulkyBookWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;

        }

        public IActionResult Index()
        {
            
            IEnumerable<Product> productList = _unitOfWork.Product.GetAll(includeProperties:"Category,ProductImages");
            return View(productList);
        }

        public IActionResult Details(int productId)
        {
            ShoppingCart cart = new()
            {
                ProductId = productId,
                Product = _unitOfWork.Product.Get(u => u.Id == productId, includeProperties: "Category,ProductImages"),
                Count = 1
            };
            return View(cart);
        }
        [HttpPost]
        [Authorize]
        public IActionResult Details(ShoppingCart shoppingCart)
        {
            //xac dinh identity cuar user de lay Id user
            var claimIdentity = (ClaimsIdentity)User.Identity;
            var userID = claimIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            shoppingCart.ApplicationUserId = userID;
            ShoppingCart cartFromDB = _unitOfWork.ShoppingCart.Get(u =>u.ApplicationUserId == userID && u.ProductId ==shoppingCart.ProductId);
            if (cartFromDB != null)
            {
                //shoppingcart exist
                cartFromDB.Count += shoppingCart.Count;
                _unitOfWork.ShoppingCart.Update(cartFromDB);
                _unitOfWork.Save();

            }
            else
            {
                //not exist
                _unitOfWork.ShoppingCart.Add(shoppingCart);
                _unitOfWork.Save();

                //đếm xem có bao nhiêu shoppingcart mà user đó sở hữu và add vào session
                HttpContext.Session.SetInt32(SD.SessionCart,
                    _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userID).Count());
            }
            
            TempData["success"] = "Cart updated successfully";
            
            //tránh lỗi sai chính tả khi gọi rediecrec("Index")
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
