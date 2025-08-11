

using FluentValidation.Results;

namespace BankingApp.Application.Exceptions
{
    public class ValidationException : Exception
    {
        public List<string> ValidationErrors { get; set; }

        public ValidationException(ValidationResult validationResult)
        {
            ValidationErrors = new List<string>();

            foreach (var validationError in validationResult.Errors)
            {
                ValidationErrors.Add(validationError.ErrorMessage);
            }
        }

        //public string ValidationErrors { get; set; }
        //public ValidationException(ValidationResult validationResult)
        //{
        //    ValidationErrors = string.Join(Environment.NewLine, validationResult.Errors);
        //}
    }
}
