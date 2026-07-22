using Eagle.BL.DTOs;

namespace Eagle.PL.Models
{
    public class AddVariantPageViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string PieceCode { get; set; } = string.Empty;
        public List<string> Colors { get; set; } = new();
        public string? SelectedColor { get; set; }
        public bool ShowNewColorForm { get; set; }
        public List<VariantDto> SizesForColor { get; set; } = new();
    }
}