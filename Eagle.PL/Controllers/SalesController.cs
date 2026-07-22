using System.Security.Claims;
using Eagle.BL.DTOs;
using Eagle.BL.Services;
using Eagle.PL.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Eagle.PL.Controllers
{
    [Authorize(Roles = "Cashier,Manager")]
    public class SalesController : Controller
    {
        private readonly ProductService _productService;
        private readonly SaleService _saleService;

        public SalesController(ProductService productService, SaleService saleService)
        {
            _productService = productService;
            _saleService = saleService;
        }

        [HttpGet]
        public async Task<IActionResult> Create(string? pieceCode)
        {
            ViewBag.PieceCode = pieceCode;
            if (!string.IsNullOrWhiteSpace(pieceCode))
            {
                var product = await _productService.LookupByPieceCodeAsync(pieceCode.Trim());
                if (product is null)
                    ModelState.AddModelError(string.Empty, "لا يوجد صنف بهذا الكود");
                return View(product);
            }
            return View((ProductLookupResult?)null);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateSaleDto dto, string pieceCode)
        {
            var cashierId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _saleService.CreateSaleAsync(dto, cashierId);

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Error!);
                var product = await _productService.LookupByPieceCodeAsync(pieceCode);
                ViewBag.PieceCode = pieceCode;
                return View(product);
            }

            TempData["Success"] = "تم تسجيل عملية البيع بنجاح";
            return RedirectToAction("Create");
        }

        [HttpGet]
        public async Task<IActionResult> History(DateTime? from, DateTime? to, Guid? cashierId)
        {
            Guid? effectiveCashierId = cashierId;
            if (!User.IsInRole("Manager"))
                effectiveCashierId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var entries = await _saleService.GetTimelineAsync(new SaleStatsFilter(from, to, effectiveCashierId));
            var vm = new TimelinePageViewModel { Entries = entries, TotalProfit = entries.Sum(e => e.ProfitAmount) };
            return View(vm);
        }

        [Authorize(Roles = "Manager")]
        [HttpGet]
        public async Task<IActionResult> Stats(DateTime? from, DateTime? to, Guid? cashierId)
        {
            var stats = await _saleService.GetStatsAsync(new SaleStatsFilter(from, to, cashierId));
            var overview = await _productService.GetStoreOverviewAsync();
            overview = overview with { RealizedRevenue = stats.TotalRevenue, RealizedProfit = stats.TotalProfit };

            var vm = new StatsPageViewModel { Stats = stats, Overview = overview };
            return View(vm);
        }
    }
}