namespace ChronoPlan.ViewModels
{
    public class MonthCellViewModel
    {
        public DateTime Date { get; set; }

        public bool IsCurrentMonth { get; set; }

        public bool IsToday { get; set; }
    }
}
