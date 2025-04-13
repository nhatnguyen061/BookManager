using Bulky.Utility;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using System.Text;

namespace BulkyBookWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
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
        public class ChatRequest
        {
            public string Message { get; set; }
        }
        #region API CALLS
        [HttpPost]
        public async Task<IActionResult> ChatAIAsync([FromBody] ChatRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { error = "Tin nhắn không được để trống." });
            }

            var client = _httpClientFactory.CreateClient();
            string apiKey = _configuration["AzureOpenAI:ApiKey"];
            string azureOpenAIUrl = "https://controlaihub4467779380.services.ai.azure.com/models/chat/completions?api-version=2024-05-01-preview"; // Đường dẫn chính xác của OpenAI API

            //RAG tay
            List<Product> products = _unitOfWork.Product.GetAll().ToList();
            var titleAndAuthorList = products.Select(p => new
            {
                p.Title,
                p.Author,
                p.Price
            }).ToList();

            string jsonTitleAuthor = JsonSerializer.Serialize(titleAndAuthorList);
            var requestBody = new
            {
                model = "DeepSeek-R1",
                messages = new[]
                {
                    new { role = "system", content = "You are an AI assistant for a Bookstore. Here is a list of books and their authors:"+jsonTitleAuthor },
                    new { role = "user", content = request.Message }
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            try
            {
                var response = await client.PostAsync(azureOpenAIUrl, jsonContent);
                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode(500, new { error = "Lỗi API OpenAI" });
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                string botReply = jsonResponse.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

                return new JsonResult(new { Response = botReply })
                {
                    ContentType = "application/json"
                };
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Lỗi server: " + ex.Message });
            }
        }
        #endregion


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
