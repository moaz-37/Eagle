using System.Security.Claims;
using Eagle.BL.DTOs;
using Eagle.BL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Eagle.PL.Controllers
{
    [Authorize(Roles = "Cashier,Manager")]
    public class PaymentsController : Controller
    {
        private readonly SaleService _saleService;
        public PaymentsController(SaleService saleService) => _saleService = saleService;

        [HttpGet]
        public async Task<IActionResult> Index(string? search)
        {
            ViewBag.Search = search;
            return View(await _saleService.GetOutstandingSalesAsync(search));
        }

        [HttpGet]
        public async Task<IActionResult> Details(int saleId)
        {
            var receipt = await _saleService.GetSaleReceiptAsync(saleId);
            if (receipt is null) return NotFound();
            return View(receipt);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPayment(AddPaymentDto dto)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _saleService.AddPaymentAsync(dto, userId);

            TempData[result.Succeeded ? "Success" : "Error"] =
                result.Succeeded ? "تم تسجيل الدفعة بنجاح" : result.Error;

            return RedirectToAction("Details", new { saleId = dto.SaleId });
        }
    }
}