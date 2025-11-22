using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using ArbolGenealogico.Modelos;
using ArbolGenealogico.Geografia;

namespace ArbolGenealogicoWPF
{
    public partial class EstadisticasWindow : WindowBaseLogica
    {
        private readonly ObservableCollection<MiembroFamilia> familiares;

        public EstadisticasWindow(ObservableCollection<MiembroFamilia> ListaFamiliares)
        {
            InitializeComponent();
            familiares = listaFamiliares;

            CalcularYMostrarEstadisticas();
        }

        // ============================
        //   LÓGICA PRINCIPAL
        // ============================
        private void CalcularYMostrarEstadisticas()
        {
            // 1. Convertir Familiar (UI) -> MiembroFamilia (modelo)
            List<MiembroFamilia> miembros = ConvertirAFamiliaModelo();

            if (miembros.Count < 2)
            {
                string msg = "Se requieren al menos 2 familiares con coordenadas válidas.";
                Farthest.Text = msg;
                Closest.Text = msg;
                Promedio.Text = "-";
                return;
            }

            // 2. Crear el grafo geográfico con tu módulo 2
            var grafo = new GrafoGeografico(miembros);

            // 3. Par más cercano
            var parCercano = grafo.ObtenerParMasCercano();
            if (parCercano != null)
            {
                Closest.Text =
                    $"{parCercano.Value.A.Nombre} y {parCercano.Value.B.Nombre} " +
                    $"- {parCercano.Value.DistanciaKm:F2} km";
            }
            else
            {
                Closest.Text = "No hay suficientes datos para calcular el par más cercano.";
            }

            // 4. Par más lejano
            var parLejano = grafo.ObtenerParMasLejano();
            if (parLejano != null)
            {
                Farthest.Text =
                    $"{parLejano.Value.A.Nombre} y {parLejano.Value.B.Nombre} " +
                    $"- {parLejano.Value.DistanciaKm:F2} km";
            }
            else
            {
                Farthest.Text = "No hay suficientes datos para calcular el par más lejano.";
            }

            // 5. Distancia promedio
            double promedioKm = grafo.ObtenerDistanciaPromedio();
            Promedio.Text = $"{promedioKm:F2} km";
        }

        // ============================
        //   CONVERSIÓN DE DATOS
        // ============================
        private List<MiembroFamilia> ConvertirAFamiliaModelo()
        {
            var lista = new List<MiembroFamilia>();

            foreach (var f in familiares)
            {
                if (f == null)
                    continue;

                if (!TryParseCoordenadas(f.Coordenadas, out double lat, out double lon))
                    continue; // ignoramos familiares sin coords válidas

                // Para el grafo la fecha no es importante, usamos una por defecto
                DateTime fecha = DateTime.Now;

                var miembro = new MiembroFamilia(
                    nombre: f.Nombre ?? string.Empty,
                    cedula: f.Cedula.ToString(),
                    fechaNacimiento: fecha,
                    estaVivo: true, // por ahora asumimos vivos
                    fotografiaRuta: f.RutaFoto ?? string.Empty,
                    latitud: lat,
                    longitud: lon
                );

                lista.Add(miembro);
            }

            return lista;
        }

        private bool TryParseCoordenadas(string? texto, out double latitud, out double longitud)
        {
            latitud = 0;
            longitud = 0;

            if (string.IsNullOrWhiteSpace(texto))
                return false;

            // Esperamos algo como: "9.93,-84.08"
            var partes = texto.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (partes.Length != 2)
                return false;

            var style = NumberStyles.Float;
            var culture = CultureInfo.InvariantCulture;

            if (!double.TryParse(partes[0].Trim(), style, culture, out latitud))
                return false;

            if (!double.TryParse(partes[1].Trim(), style, culture, out longitud))
                return false;

            return true;
        }
    }
}
