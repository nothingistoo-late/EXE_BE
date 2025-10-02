using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.Gemini
{
    public class GenerateWishRequestDto
    {
        public string Receiver { get; set; } = string.Empty;
        public string Occasion { get; set; } = string.Empty;
        public string MainWish { get; set; } = string.Empty;
        public string Custom { get; set; } = string.Empty;
    }

}
