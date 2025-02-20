using System.Text;

namespace ProjectService.Domain.Extensions;

public static class StringExtensions
{
    public static bool EqualsIgnoreCase(this string str1, string str2)
    {
        return str1.Equals(str2, StringComparison.OrdinalIgnoreCase);
    }
    
    public static string ToSnakeCase(this string str)
    {
        if (string.IsNullOrEmpty(str))
        {
            return str;
        }
        
        var builder = new StringBuilder();
        
        for (var i = 0; i < str.Length; i++)
        {
            if (char.IsUpper(str[i]))
            {
                if (i > 0)
                {
                    builder.Append('_');
                }
                
                builder.Append(char.ToLower(str[i]));
            }
            else
            {
                builder.Append(str[i]);
            }
        }
        
        return builder.ToString();
    }
}