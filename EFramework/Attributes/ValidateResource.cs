using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace Spolis.Attributes
{

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class ValidateResource : ValidationAttribute
    {
        public static readonly string DefaultRequredError = "";
        public static readonly string DefaultMaxLengthError = "";
        public static readonly string DefaultMinLengthError = "";

        public ValidateResource(string key = null, bool required = true, int maxLenght = -1, int minLenght = -1, [CallerMemberName] string name = null)
        {
            this.Key = key;

            this.Requred = required;
            this.MaxLength = maxLenght;
            this.MinLength = minLenght;
            this.Name = name;
        }

        public string Key { get; }

        public bool Requred { get; }
        public int MaxLength { get; }
        public int MinLength { get; }
        public string Name { get; }

        private string message = null;

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            IsValid(value);
            if (message == null) return ValidationResult.Success;
            return new ValidationResult(message, new[] { validationContext.MemberName });
        }

        public override bool IsValid(object value)
        {
            message = null;
            var DisplayName = ResourceHelper.GetResourceValue(Key, Name);
            if (Requred && (value == null || (value.GetType() == typeof(string) && string.IsNullOrWhiteSpace((string)value))))
            {
                message = string.Format(DefaultRequredError, DisplayName);
            }
            else if (MinLength >= 0 && (value == null || ((string)value).Length < MinLength))
            {
                message = string.Format(DefaultMinLengthError, DisplayName, MinLength.ToString());
            }
            else if (MaxLength >= 0 && (value != null && (value.ToString()).Length > MaxLength))
            {
                message = string.Format(DefaultMaxLengthError, DisplayName, MaxLength.ToString());
            }

            return (message == null);
        }

        public override string FormatErrorMessage(string name)
        {
            return message;
        }

        public void SetEditorAttributes(Dictionary<string, object> attributes)
        {
            if (Requred || MaxLength >= 0 || MinLength >= 0)
            {

                var DisplayName = ResourceHelper.GetResourceValue(Key, Name);
               
               
                if (Requred)
                {
                    attributes.Add("data-val", "true");
                    attributes.Add("data-val-required", string.Format(DefaultRequredError, DisplayName));
                }
                if (MaxLength >= 0)
                {
                    attributes.Add("maxlength", MaxLength);
                    attributes.Add("data-val-maxlength-max", MaxLength);
                    attributes.Add("data-val-maxlength", string.Format(DefaultMaxLengthError, DisplayName, MaxLength.ToString()));
                }
                if (MinLength >= 0)
                {
                    attributes.Add("minlength", MinLength);
                    attributes.Add("data-val-minlength-min", MinLength);
                    attributes.Add("data-val-minlength", string.Format(DefaultMinLengthError, DisplayName, MinLength.ToString()));
                }
            }
        }


    }

}
