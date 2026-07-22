using System.Security.Claims;
using Eagle.BL.DTOs;
using Eagle.BL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Eagle.PL.Controllers
{
    [Authorize(Roles = "Cashier,Manager")]
    public class ReturnsController : Controller
    {
        private readonly SaleService _saleService;
        public ReturnsController(SaleService saleService) => _saleService = saleService;

        [HttpGet]
        public async Task<IActionResult> Create(string? pieceCode)
        {
            ViewBag.PieceCode = pieceCode;
            var items = new List<SaleItemForReturnDto>();

            if (!string.IsNullOrWhiteSpace(pieceCode))
            {
                items = await _saleService.SearchSaleItemsByPieceCodeAsync(pieceCode.Trim());
                if (!items.Any())
                    ModelState.AddModelError(string.Empty, "لا توجد عمليات بيع قابلة للإرجاع بهذا الكود");
            }

            return View(items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateReturnDto dto, string pieceCode)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _saleService.CreateReturnAsync(dto, userId);

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Error!);
                var items = await _saleService.SearchSaleItemsByPieceCodeAsync(pieceCode);
                ViewBag.PieceCode = pieceCode;
                return View(items);
            }

            TempData["Success"] = "تم تسجيل عملية الإرجاع بنجاح";
            return RedirectToAction("Create");
        }
    }
}