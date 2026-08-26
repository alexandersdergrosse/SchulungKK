using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;

namespace SchulungKK.Pages
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]
    public class ErrorModel : PageModel
    {
        public string? RequestId { get; set; }

        public int? HttpStatusCode { get; set; }

        public bool ShowRequestId => !string.IsNullOrWhiteSpace(RequestId);

        public void OnGet(int? statusCode = null)
        {
            HttpStatusCode = statusCode;

            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        }
    }
}