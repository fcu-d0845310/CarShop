using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace CarShop.Infrastructure.Security
{
    public class TWIdAttribute : ValidationAttribute
    {
        private static readonly Regex IdRegex = new(@"^[A-Z][12]\d{8}$", RegexOptions.Compiled);

        private const string Letters = "ABCDEFGHJKLMNPQRSTUVXYWZIO";

        public override bool IsValid(object? value)
        {
            string? input = Convert.ToString(value);

            if (string.IsNullOrEmpty(input))
            {
                return true;
            }

            if(!IdRegex.IsMatch(input))
            {
                return false;
            }

            int letterNum = Letters.IndexOf(input[0]) + 10;
            int sum = (letterNum/10) + (letterNum%10) * 9;

            for(int i = 1; i < input.Length - 1; i++)
            {
                sum = sum + ((input[i] - '0') * (9 - i));
            }

            sum = sum + (input[9] - '0');

            if(sum % 10 == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
