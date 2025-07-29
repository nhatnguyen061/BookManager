using Bulky.Utility;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BuklyBookApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public OrderController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public IActionResult GetAll(string status)
        {
            IEnumerable<OrderHeader> objOrderHeader;

            if (User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
            {
                objOrderHeader = _unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser").ToList();
            }
            else
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
                objOrderHeader = _unitOfWork.OrderHeader.GetAll(u => u.ApplicationUserId == userId, includeProperties: "ApplicationUser");
            }

            switch (status)
            {
                case "pending":
                    objOrderHeader = objOrderHeader.Where(u => u.PaymentStatus == SD.PaymentStatusDelayPayment);
                    break;
                case "inprocess":
                    objOrderHeader = objOrderHeader.Where(u => u.OrderStatus == SD.StatusInProcess);
                    break;
                case "completed":
                    objOrderHeader = objOrderHeader.Where(u => u.OrderStatus == SD.StatusShipped);
                    break;
                case "approved":
                    objOrderHeader = objOrderHeader.Where(u => u.OrderStatus == SD.StatusApproved);
                    break;
                default:
                    // không làm gì, trả về tất cả
                    break;
            }

            // Dùng Ok() là chuẩn cho ApiController, nó sẽ tự động serialize thành JSON
            return Ok(new { data = objOrderHeader });
        }
    }
}
