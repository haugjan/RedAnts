using System.Text;
using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.Admin;

public sealed record TicketExportRow(
    string CardNo, string? Bundle, string? Category, CardHolder Holder, int? Admissions, string Link);

public static class TicketExportCsv
{
    private const string Header =
        "Karten-Nr;Bundle;Kategorie;Anzahl;Firma;Anrede;Name;Vorname;Strasse;Adresszusatz;PLZ;Ort;Land;E-Mail;Telefon;Geburtsdatum;Link";

    public static byte[] Build(IEnumerable<TicketExportRow> rows)
    {
        var sb = new StringBuilder();
        sb.Append(Header).Append("\r\n");
        foreach (var r in rows)
        {
            var h = r.Holder;
            sb.Append(Csv(r.CardNo)).Append(';')
              .Append(Csv(r.Bundle)).Append(';')
              .Append(Csv(r.Category)).Append(';')
              .Append(Csv(r.Admissions?.ToString())).Append(';')
              .Append(Csv(h.Company)).Append(';')
              .Append(Csv(h.Salutation)).Append(';')
              .Append(Csv(h.LastName)).Append(';')
              .Append(Csv(h.FirstName)).Append(';')
              .Append(Csv(h.Street)).Append(';')
              .Append(Csv(h.AddressLine2)).Append(';')
              .Append(Csv(h.PostalCode)).Append(';')
              .Append(Csv(h.City)).Append(';')
              .Append(Csv(h.Country)).Append(';')
              .Append(Csv(h.Email)).Append(';')
              .Append(Csv(h.Phone)).Append(';')
              .Append(Csv(h.Birthday?.ToString("dd.MM.yyyy"))).Append(';')
              .Append(Csv(r.Link)).Append("\r\n");
        }
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    private static string Csv(string? value)
    {
        var s = Neutralize(value ?? "");
        return s.IndexOfAny([';', '"', '\r', '\n']) >= 0 ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;
    }

    private static string Neutralize(string value) =>
        value.Length > 0 && value[0] is '=' or '+' or '-' or '@' or '\t' or '\r' ? "'" + value : value;
}
