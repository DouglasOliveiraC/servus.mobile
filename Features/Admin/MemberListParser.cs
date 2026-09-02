using System.Text.RegularExpressions;

namespace Servus.Mobile.Features.Admin;

public record ParsedMemberRow(string Name, string? Email, string? Phone, int Line);

public record ParsedMemberListError(int Line, string Text, string Reason);

public record ParsedMemberList(
    IReadOnlyList<ParsedMemberRow> Rows,
    IReadOnlyList<ParsedMemberListError> Errors,
    IReadOnlyList<string> DuplicateNames);

/// <summary>Port of Servus.Web's parseMemberList.ts — keep both in sync when the parsing rules change.</summary>
public static partial class MemberListParser
{
    private const int MaxNameLength = 100;
    private const int MaxEmailLength = 255;
    private const int MaxPhoneLength = 30;

    private static readonly HashSet<string> HeaderLabels = ["nome", "name", "membro", "pessoa"];

    [GeneratedRegex(@"^[^\s@]+@[^\s@]+\.[^\s@]+$")]
    private static partial Regex EmailPattern();

    private static string[] SplitColumns(string line)
    {
        var separator = new[] { ';', '\t', ',' }.FirstOrDefault(c => line.Contains(c));
        return separator == default ? [line] : line.Split(separator);
    }

    private static bool IsHeaderRow(string[] columns) =>
        HeaderLabels.Contains(columns.Length > 0 ? columns[0].Trim().ToLowerInvariant() : "");

    private static bool LooksLikeEmail(string value) => EmailPattern().IsMatch(value);

    private static (string? Email, string? Phone) AssignContactColumns(string[] columns)
    {
        var values = columns.Skip(1).Select(c => c.Trim()).Where(c => c.Length > 0).ToList();
        var email = values.FirstOrDefault(LooksLikeEmail);
        var phone = values.FirstOrDefault(v => v != email);
        return (email, phone);
    }

    public static ParsedMemberList Parse(string input)
    {
        var rows = new List<ParsedMemberRow>();
        var errors = new List<ParsedMemberListError>();
        var duplicateNames = new List<string>();
        var seenNames = new HashSet<string>();

        var lines = input.Split('\n');

        for (var index = 0; index < lines.Length; index++)
        {
            var line = index + 1;
            var text = lines[index].TrimEnd('\r').Trim();
            if (text.Length == 0) continue;

            var columns = SplitColumns(text);
            if (index == 0 && IsHeaderRow(columns)) continue;

            var name = columns[0].Trim();
            if (name.Length == 0)
            {
                errors.Add(new ParsedMemberListError(line, text, "Sem nome na primeira coluna."));
                continue;
            }
            if (name.Length > MaxNameLength)
            {
                errors.Add(new ParsedMemberListError(line, text, $"Nome com mais de {MaxNameLength} caracteres."));
                continue;
            }

            var key = name.ToLowerInvariant();
            if (!seenNames.Add(key))
            {
                duplicateNames.Add(name);
                continue;
            }

            var (email, phone) = AssignContactColumns(columns);
            if (email is not null && email.Length > MaxEmailLength)
            {
                errors.Add(new ParsedMemberListError(line, text, $"E-mail com mais de {MaxEmailLength} caracteres."));
                continue;
            }
            if (phone is not null && phone.Length > MaxPhoneLength)
            {
                errors.Add(new ParsedMemberListError(line, text, $"Telefone com mais de {MaxPhoneLength} caracteres."));
                continue;
            }

            rows.Add(new ParsedMemberRow(name, email, phone, line));
        }

        return new ParsedMemberList(rows, errors, duplicateNames);
    }
}
