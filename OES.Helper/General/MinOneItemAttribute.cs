using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OES.Helper.General
{
    public class MinOneItemAttribute: ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var list = value as List<string>;
            if (list != null && list.Count > 0)
            {
                return ValidationResult.Success;
            }
            return new ValidationResult("At least one must be selected.");
        }
    }
}
