using Microsoft.AspNetCore.Mvc.RazorPages;
using SchulungKK.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace SchulungKK.Pages
{
    public class DbTestModel : PageModel
    {
        private readonly SchulungenDbContext? _context;
        private readonly IConfiguration _configuration;

        public DbTestModel(SchulungenDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public string Message { get; set; } = string.Empty;
        public int BenutzerAnzahl { get; set; }
        public string ConnectionString { get; set; } = string.Empty;
        public string ConnectionStringFromConfig { get; set; } = string.Empty;
        public bool ContextIsNull { get; set; }
        public string DetailedError { get; set; } = string.Empty;
        public bool CanConnect { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                ContextIsNull = (_context == null);

                // Connection String aus appsettings.json
                ConnectionStringFromConfig = _configuration.GetConnectionString("SchulungenDb") ?? "NICHT GEFUNDEN!";

                if (_context == null)
                {
                    Message = "FEHLER: _context ist NULL!";
                    return;
                }

                // Connection String aus DbContext
                ConnectionString = _context.Database.GetConnectionString() ?? "NICHT GEFUNDEN!";

                // Verbindung testen
                Message = "Verbindung wird getestet...";
                CanConnect = await _context.Database.CanConnectAsync();

                if (CanConnect)
                {
                    Message = "Datenbankverbindung erfolgreich!";

                    // Versuche Benutzer zu zählen
                    try
                    {
                        if (_context.Benutzer != null)
                        {
                            BenutzerAnzahl = await _context.Benutzer.CountAsync();
                        }
                        else
                        {
                            Message += " | Benutzer-Tabelle ist null!";
                        }
                    }
                    catch (Exception countEx)
                    {
                        Message += " | Fehler beim Zählen der Benutzer";
                        DetailedError = $"Count-Fehler: {countEx.Message}";
                    }
                }
                else
                {
                    Message = "Kann nicht zur Datenbank verbinden!";
                }
            }
            catch (Exception ex)
            {
                Message = "Fehler beim Verbinden";
                DetailedError = $"Fehler: {ex.Message}";

                if (ex.InnerException != null)
                {
                    DetailedError += $"\n\nInner Exception: {ex.InnerException.Message}";
                }

                if (ex.InnerException?.InnerException != null)
                {
                    DetailedError += $"\n\nInner Inner Exception: {ex.InnerException.InnerException.Message}";
                }
            }
        }
    }
}
