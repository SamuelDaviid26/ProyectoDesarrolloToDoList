namespace ToDoApi.BackgroundServices;

public class OverdueReviewOptions
{
    public const string SectionName = "OverdueReview";

    public bool Enabled { get; set; } = true;

    public int IntervalSeconds { get; set; } = 60;
}