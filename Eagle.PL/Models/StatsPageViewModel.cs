using Eagle.BL.DTOs;

namespace Eagle.PL.Models
{
    public class StatsPageViewModel
    {
        public SaleStatsSummary Stats { get; set; } = null!;
        public StoreOverviewDto Overview { get; set; } = null!;
    }

    public class TimelinePageViewModel
    {
        public List<SaleTimelineEntryDto> Entries { get; set; } = new();
        public decimal TotalProfit { get; set; }
    }
}