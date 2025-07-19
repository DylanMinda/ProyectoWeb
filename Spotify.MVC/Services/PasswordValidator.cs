namespace Spotify.MVC.Services
{
    using System.Text.RegularExpressions;

    public class PasswordValidator
    {
        public static class Requirements
        {
            public const int MIN_LENGTH = 8;
            public const int MAX_LENGTH = 128;
        }

        public static ValidationResult ValidatePassword(string password)
        {
            var result = new ValidationResult();

            if (string.IsNullOrWhiteSpace(password))
            {
                result.AddError("La contraseña es obligatoria.");
                return result;
            }

            // Validar longitud
            if (password.Length < Requirements.MIN_LENGTH)
            {
                result.AddError($"La contraseña debe tener al menos {Requirements.MIN_LENGTH} caracteres.");
            }

            if (password.Length > Requirements.MAX_LENGTH)
            {
                result.AddError($"La contraseña no puede tener más de {Requirements.MAX_LENGTH} caracteres.");
            }

            // Validar caracteres requeridos
            if (!HasLowercase(password))
            {
                result.AddError("La contraseña debe contener al menos una letra minúscula.");
            }

            if (!HasUppercase(password))
            {
                result.AddError("La contraseña debe contener al menos una letra mayúscula.");
            }

            if (!HasNumber(password))
            {
                result.AddError("La contraseña debe contener al menos un número.");
            }

            if (!HasSpecialCharacter(password))
            {
                result.AddError("La contraseña debe contener al menos un carácter especial (!@#$%^&*()_+-=[]{};':\"\\|,.<>/?).");
            }

            // Validar patrones débiles
            if (HasWeakPatterns(password))
            {
                result.AddError("La contraseña contiene patrones comunes que la hacen vulnerable. Por favor, elija una contraseña más segura.");
            }

            return result;
        }

        public static ValidationResult ValidateEmail(string email)
        {
            var result = new ValidationResult();

            if (string.IsNullOrWhiteSpace(email))
            {
                result.AddError("El correo electrónico es obligatorio.");
                return result;
            }

            if (!IsValidEmailFormat(email))
            {
                result.AddError("Por favor, ingrese un correo electrónico válido.");
            }

            return result;
        }

        public static ValidationResult ValidateName(string name)
        {
            var result = new ValidationResult();

            if (string.IsNullOrWhiteSpace(name))
            {
                result.AddError("El nombre es obligatorio.");
                return result;
            }

            if (name.Trim().Length < 2)
            {
                result.AddError("El nombre debe tener al menos 2 caracteres.");
            }

            if (name.Length > 100)
            {
                result.AddError("El nombre no puede tener más de 100 caracteres.");
            }

            return result;
        }

        public static ValidationResult ValidatePasswordConfirmation(string password, string confirmPassword)
        {
            var result = new ValidationResult();

            if (string.IsNullOrWhiteSpace(confirmPassword))
            {
                result.AddError("La confirmación de contraseña es obligatoria.");
                return result;
            }

            if (password != confirmPassword)
            {
                result.AddError("Las contraseñas no coinciden.");
            }

            return result;
        }

        public static List<string> GetPasswordRequirements()
        {
            return new List<string>
        {
            $"Al menos {Requirements.MIN_LENGTH} caracteres de longitud",
            "Al menos una letra minúscula (a-z)",
            "Al menos una letra mayúscula (A-Z)",
            "Al menos un número (0-9)",
            "Al menos un carácter especial (!@#$%^&*()_+-=[]{};':\"\\|,.<>/?)",
            "No debe contener patrones comunes como '123456', 'password', etc.",
            "No debe tener más de dos caracteres idénticos consecutivos"
        };
        }

        #region Private Methods

        private static bool HasLowercase(string password)
        {
            return Regex.IsMatch(password, @"[a-z]");
        }

        private static bool HasUppercase(string password)
        {
            return Regex.IsMatch(password, @"[A-Z]");
        }

        private static bool HasNumber(string password)
        {
            return Regex.IsMatch(password, @"[0-9]");
        }

        private static bool HasSpecialCharacter(string password)
        {
            return Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]");
        }

        private static bool HasWeakPatterns(string password)
        {
            var commonPatterns = new[]
            {
            @"(.)\1{2,}", // Tres o más caracteres repetidos consecutivos
            @"123|234|345|456|567|678|789|890|abc|bcd|cde|def|efg|fgh|ghi|hij|ijk|jkl|klm|lmn|mno|nop|opq|pqr|qrs|rst|stu|tuv|uvw|vwx|wxy|xyz",
            @"password|contraseña|12345|qwerty|admin|usuario|user|pass"
        };

            return commonPatterns.Any(pattern => Regex.IsMatch(password.ToLower(), pattern));
        }

        private static bool IsValidEmailFormat(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }

    public class ValidationResult
    {
        public bool IsValid => !Errors.Any();
        public List<string> Errors { get; private set; } = new List<string>();
        public string FirstError => Errors.FirstOrDefault() ?? "";

        public void AddError(string error)
        {
            Errors.Add(error);
        }

        public void AddErrors(IEnumerable<string> errors)
        {
            Errors.AddRange(errors);
        }
    }
}
