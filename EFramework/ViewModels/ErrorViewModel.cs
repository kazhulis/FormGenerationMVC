using System.Collections.Generic;

namespace Spolis.ViewModels
{
    public class ErrorViewModel
    {
        public string RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        public string ErrorMessage { get; set; }

        public List<string> ErrorMessages { get; set; }
    }
}