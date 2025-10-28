using System;
using System.ComponentModel.DataAnnotations;

namespace DTOs.BoxType.Request
{
    public class UpdateBoxTypeRequest
    {

        // Only update when provided (non-null and not empty/whitespace)
        public string? Name { get; set; }

        // Only update when provided (non-null and not empty/whitespace)
        public string? Description { get; set; }

        // Only update when provided
        [Range(0, double.MaxValue, ErrorMessage = "Price must be greater than or equal to 0")]
        public double? Price { get; set; }

        // Only update when provided
        public Guid? ParentID { get; set; }
    }
}