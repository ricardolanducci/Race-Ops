namespace RaceOps.Infrastructure.Database.Models;

public class UserPreferencesModel
{
    public int Id { get; set; }
    public int? SelectedRaceId { get; set; }
    public string Layout { get; set; } = "{}";
}
