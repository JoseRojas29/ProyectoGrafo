#nullable enable
using System;
using System.ComponentModel;
using System.Windows.Media;
using System.Windows.Media.Imaging;

public class Familiar : INotifyPropertyChanged
{
    private string _nombre = string.Empty;
    private string _cedula = string.Empty;
    private string _coordenadas = string.Empty;
    private string _rutaFoto = string.Empty;

    private DateTime? _fechaNacimiento;
    private DateTime? _fechaFallecimiento;
    private bool _isDeceased;

    public string Nombre
    {
        get => _nombre;
        set { _nombre = value ?? string.Empty; OnPropertyChanged(nameof(Nombre)); }
    }

    public string Cedula
    {
        get => _cedula;
        set { _cedula = value ?? string.Empty; OnPropertyChanged(nameof(Cedula)); }
    }

    public string Coordenadas
    {
        get => _coordenadas;
        set { _coordenadas = value ?? string.Empty; OnPropertyChanged(nameof(Coordenadas)); }
    }

    public DateTime? FechaNacimiento
    {
        get => _fechaNacimiento;
        set { _fechaNacimiento = value; OnPropertyChanged(nameof(FechaNacimiento)); RecalcularEdades(); }
    }

    public DateTime? FechaFallecimiento
    {
        get => _fechaFallecimiento;
        set { _fechaFallecimiento = value; _isDeceased = value.HasValue; OnPropertyChanged(nameof(FechaFallecimiento)); OnPropertyChanged(nameof(IsDeceased)); RecalcularEdades(); }
    }

    public bool IsDeceased
    {
        get => _isDeceased;
        set { _isDeceased = value; if (!value) FechaFallecimiento = null; OnPropertyChanged(nameof(IsDeceased)); }
    }

    public int? Edad
    {
        get
        {
            if (!FechaNacimiento.HasValue) return null;
            var referencia = IsDeceased && FechaFallecimiento.HasValue ? FechaFallecimiento.Value.Date : DateTime.Today;
            return CalcularAnios(FechaNacimiento.Value.Date, referencia);
        }
    }

    public int? EdadAlFallecer => (FechaNacimiento.HasValue && FechaFallecimiento.HasValue)
        ? CalcularAnios(FechaNacimiento.Value.Date, FechaFallecimiento.Value.Date)
        : null;

    public string RutaFoto
    {
        get => _rutaFoto;
        set { _rutaFoto = value ?? string.Empty; OnPropertyChanged(nameof(RutaFoto)); OnPropertyChanged(nameof(FotoImage)); }
    }

    public ImageSource? FotoImage
    {
        get
        {
            if (string.IsNullOrWhiteSpace(RutaFoto)) return null;
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(RutaFoto, UriKind.RelativeOrAbsolute);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                return bmp;
            }
            catch { return null; }
        }
    }

    private void RecalcularEdades()
    {
        OnPropertyChanged(nameof(Edad));
        OnPropertyChanged(nameof(EdadAlFallecer));
    }

    private static int CalcularAnios(DateTime nacimiento, DateTime referencia)
    {
        var years = referencia.Year - nacimiento.Year;
        if (nacimiento.Date > referencia.AddYears(-years)) years--;
        return Math.Max(0, years);
    }

    public event PropertyChangedEventHandler? PropertyChanged; // nullable para coincidir con la interfaz
    protected void OnPropertyChanged(string nombre) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nombre));
}
