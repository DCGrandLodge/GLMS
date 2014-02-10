using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLMS.BLL.Entities
{
    public enum ResetPasswordResult { Failure, WeakPassword, Success };

    public class Password
    {
        public static int MinLength { get; set; }
        public static bool RequireUpper { get; set; }
        public static bool RequireLower { get; set; }
        public static bool RequireSymbol { get; set; }
        public static bool RequireNumeric { get; set; }
        public static bool RequireLetter { get { return _requireLetter || RequireUpper || RequireLower; } set { _requireLetter = value; } }
        private static bool _requireLetter;

        public byte[] Encrypted { get; set; }
        public string PlainTextPassword { set { if (value == null) { value = ""; } Encrypted = value.Encrypt(); } }
        public byte[] Temporary { get; set; }
        public string PlainTextTemporary { set { if (value == null) { value = ""; } Temporary = value.Encrypt(); TempExpiration = DateTime.Now.AddDays(1); } }
        public DateTime? TempExpiration { get; set; }
        public bool ForceChange { get; set; }


        static Password()
        {
            MinLength = 8;
            RequireLetter = true;
            RequireNumeric = true;
        }

        public ResetPasswordResult TryChangePassword(string email, string oldPassword, string newPassword)
        {
            if (oldPassword.CompareToEncrypted(Encrypted) || (Temporary != null && oldPassword.CompareToEncrypted(Temporary)))
            {
                var result = CheckPasswordStrength(email, newPassword);
                if (result == ResetPasswordResult.Success)
                {
                    PlainTextPassword = newPassword;
                }
                return result;
            }
            return ResetPasswordResult.Failure;
        }

        public static ResetPasswordResult CheckPasswordStrength(string email, string password)
        {
            // Check length
            if (String.IsNullOrEmpty(password) || (password.Length < MinLength)) return ResetPasswordResult.WeakPassword;

            // Check composition
            bool hasUpper = false;
            bool hasLower = false;
            bool hasSymbol = false;
            bool hasLetter = false;
            bool hasNumeric = false;
            foreach (char ch in password)
            {
                if (Char.IsDigit(ch)) hasNumeric = true;
                else if (Char.IsUpper(ch)) { hasLetter = true; hasUpper = true; }
                else if (Char.IsLower(ch)) { hasLetter = true; hasLower = true; }
                else if (Char.IsSymbol(ch) || Char.IsPunctuation(ch)) hasSymbol = true;
            }
            if (!((hasUpper || !RequireUpper) && 
                  (hasLower || !RequireLower) &&
                  (hasLetter || !RequireLetter) &&
                  (hasNumeric || !RequireNumeric) &&
                  (hasSymbol || !RequireSymbol)))
                return ResetPasswordResult.WeakPassword;

            // Check email
            email = (email ?? "").ToLower();
            string emailPart = email.Split('@')[0];
            string checkPW = password.ToLower();
            if (checkPW.Contains(email) || checkPW.Contains(emailPart)) return ResetPasswordResult.WeakPassword;

            // If we haven't failed yet, password is acceptable
            return ResetPasswordResult.Success;
        }

        public static string CreateRandomPassword(int passwordLength = 0)
        {
            if (passwordLength < MinLength)
            {
                passwordLength = MinLength;
            }

            const string lower = "abcdefghijkmnopqrstuvwxyz";
            const string upper = "ABCDEFGHJKLMNOPQRSTUVWXYZ";
            const string letters = lower + upper;
            const string numbers = "1234567890";
            const string symbols = "!@#$%^*()-=+?";
            StringBuilder builder = new StringBuilder();
            StringBuilder password = new StringBuilder();
            Random rd = new Random();
            if (RequireLower)
            {
                password.Append(lower[rd.Next(0, lower.Length)]);
                builder.Append(lower);
            }
            if (RequireUpper)
            {
                password.Append(upper[rd.Next(0, upper.Length)]);
                builder.Append(upper);
            }
            if (RequireLetter && !(RequireLower || RequireUpper))
            {
                password.Append(letters[rd.Next(0, letters.Length)]);
                builder.Append(letters);
            }
            if (RequireNumeric)
            {
                password.Append(numbers[rd.Next(0, numbers.Length)]);
                builder.Append(numbers);
            }
            if (RequireSymbol)
            {
                password.Append(symbols[rd.Next(0, symbols.Length)]);
                builder.Append(symbols);
            }
            string allowedChars = builder.ToString();
            char[] chars = new char[passwordLength];

            for (int i = password.Length; i < passwordLength; i++)
            {
                password.Append(allowedChars[rd.Next(0, allowedChars.Length)]);
            }

            return new string(chars);
        }
    }
}
