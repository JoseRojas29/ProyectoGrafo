using System;
using System.ComponentModel;

public class Familiar : INotifyPropertyChanged
{
    private string _nombre = string.Empty;
    private int _cedula;
    private int _edad;
    private string _coordenadas = string.Empty;
    private string _rutaFoto = string.Empty;
    private string _parentesco = string.Empty;
    private DateTime? _fechaNacimiento;

    public string Nombre
    {
        get => _nombre;
        set { _nombre = value ?? string.Empty; OnPropertyChanged(nameof(Nombre)); }
    }

    public int Cedula
    {
        get => _cedula;
        set { _cedula = value; OnPropertyChanged(nameof(Cedula)); }
    }

    public int Edad
    {
        get => _edad;
        set { _edad = value; OnPropertyChanged(nameof(Edad)); }
    }

    public string Parentesco
    {
        get => _parentesco;
        set { _parentesco = value ?? string.Empty; OnPropertyChanged(nameof(Parentesco)); }
    }

    public string Coordenadas
    {
        get => _coordenadas;
        set { _coordenadas = value ?? string.Empty; OnPropertyChanged(nameof(Coordenadas)); }
    }

    public DateTime? FechaNacimiento
    {
        get => _fechaNacimiento;
        set { _fechaNacimiento = value; OnPropertyChanged(nameof(FechaNacimiento)); }
    }

    public string RutaFoto
    {
        get => _rutaFoto;
        set { _rutaFoto = value ?? string.Empty; OnPropertyChanged(nameof(RutaFoto)); }
    }

    // Implementación de INotifyPropertyChanged
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
