using Eagle.BL.DTOs;
using Eagle.BL.Services;
using Eagle.PL.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Eagle.PL.Controllers
{
    [Authorize(Roles = "Manager")]
    public class ProductsController : Controller
    {
        private readonly ProductService _productService;
        private readonly SaleService _saleService;
        private readonly OverrideCodeService _overrideCodeService;

        public ProductsController(ProductService productService, SaleService saleService, OverrideCodeService overrideCodeService)
        {
            _productService = productService;
            _saleService = saleService;
            _overrideCodeService = overrideCodeService;
        }

        public async Task<IActionResult> Index(string? search)
        {
            ViewBag.Search = search;
            ViewBag.TodayOverrideCode = await _overrideCodeService.GetOrCreateTodayCodeAsync();
            return View(await _productService.GetAllAsync(search));
        }

        [HttpGet]
        public IActionResult Create() => View(new CreateProductDto("", null, 0, 0));

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateProductDto dto)
        {
            if (!ModelState.IsValid) return View(dto);

            var id = await _productService.CreateAsync(dto);
            var product = await _productService.GetByIdAsync(id);
            TempData["Success"] = $"تم إنشاء الصنف بنجاح - كود الصنف: {product!.PieceCode}";
            return RedirectToAction("AddVariant", new { productId = id });
        }

        [HttpGet]
        public async Task<IActionResult> AddVariant(int productId, string? color, bool newColor = false)
        {
            var product = await _productService.GetByIdAsync(productId);
            if (product is null) return NotFound();

            var vm = new AddVariantPageViewModel
            {
                ProductId = productId,
                ProductName = product.Name,
                PieceCode = product.PieceCode,
                Colors = await _productService.GetColorsAsync(productId),
                SelectedColor = color,
                ShowNewColorForm = newColor
            };

            if (!string.IsNullOrWhiteSpace(color))
                vm.SizesForColor = await _productService.GetSizesForColorAsync(productId, color);

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddVariant(AddVariantDto dto)
        {
            await _productService.AddOrIncrementVariantAsync(dto);
            TempData["Success"] = "تم تحديث الكمية بنجاح";
            return RedirectToAction("AddVariant", new { productId = dto.ProductId, color = dto.Color });
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var product = await _productService.GetByIdAsync(id);
            if (product is null)
                return NotFound();

            var saleHistory = await _saleService.GetSaleHistoryForProductAsync(id);
            var returnHistory = await _saleService.GetReturnHistoryForProductAsync(id);

            var detail = new ProductDetailDto
            (
                product.Id, product.PieceCode, product.Name, product.Brand,
                product.BuyPrice, product.SellPrice, product.CreatedAt,
                product.Variants.Select(v => new VariantDto(v.Id, v.Color, v.Size, v.StockQuantity)).ToList(),
                saleHistory,
                returnHistory
            );

            return View(detail);
        }

        [HttpGet]
        public async Task<IActionResult> PrintLabel(int id)
        {
            var product = await _productService.GetByIdAsync(id);
            if (product is null) return NotFound();

            return View(product);
        }
    }
}