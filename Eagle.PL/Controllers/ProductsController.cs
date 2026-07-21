using Eagle.BL.DTOs;
using Eagle.BL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Eagle.PL.Controllers
{
    [Authorize(Roles = "Manager")]
    public class ProductsController : Controller
    {
        private readonly ProductService _productService;
        public ProductsController(ProductService productService) => _productService = productService;

        public async Task<IActionResult> Index() => View(await _productService.GetAllAsync());

        [HttpGet]
        public IActionResult Create() => View(new CreateProductDto("", "", null, 0, 0));

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateProductDto dto)
        {
            if (!ModelState.IsValid) return View(dto);
            try
            {
                var id = await _productService.CreateAsync(dto);
                return RedirectToAction("AddVariant", new { productId = id });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(dto);
            }
        }

        [HttpGet]
        public IActionResult AddVariant(int productId) => View(new AddVariantDto(productId, "", "", 0));

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddVariant(AddVariantDto dto)
        {
            try
            {
                await _productService.AddVariantAsync(dto);
                TempData["Success"] = "تمت إضافة اللون/المقاس بنجاح";
                return RedirectToAction("AddVariant", new { productId = dto.ProductId });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(dto);
            }
        }
    }
}