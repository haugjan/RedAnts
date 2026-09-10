namespace RedAnts.Ticketing.Domain.Sales;

public static class Birthdate
{
    public const int EarliestYear = 1900;

    public static DateOnly? Validate(DateOnly? value, string field = "birthday", TimeProvider? time = null)
    {
        if (value is not { } day) return null;
        if (day > SwissTime.TodayOf(time))
            throw new ValidationException(field, "Das Geburtsdatum darf nicht in der Zukunft liegen.");
        if (day.Year < EarliestYear)
            throw new ValidationException(field, "Bitte ein gültiges Geburtsdatum angeben.");
        return day;
    }
}
