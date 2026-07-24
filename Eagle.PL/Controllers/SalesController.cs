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
        public IActionResult Create()
        {
            return View();
        }

        // JSON lookup used by the client-side cart to fetch product + variants for a piece code
        [HttpGet]
        public async Task<IActionResult> LookupJson(string pieceCode)
        {
            if (string.IsNullOrWhiteSpace(pieceCode))
                return Json(null);

            var product = await _productService.LookupByPieceCodeAsync(pieceCode.Trim());
            return Json(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromBody] CreateSaleDto dto)
        {
            var cashierId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _saleService.CreateSaleAsync(dto, cashierId);

            if (!result.Succeeded)
                return Json(new { succeeded = false, error = result.Error });

            TempData["Success"] = "تم تسجيل عملية البيع بنجاح";
            TempData["LastSaleId"] = result.SaleId;

            return Json(new { succeeded = true, saleId = result.SaleId });
        }

        [HttpGet]
        public async Task<IActionResult> PrintReceipt(int id)
        {
            var receipt = await _saleService.GetSaleReceiptAsync(id);
            if (receipt is null) return NotFound();

            return View(receipt);
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