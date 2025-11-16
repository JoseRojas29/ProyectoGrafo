using System.Collections.ObjectModel;
using System.Windows;

namespace ArbolGenealogicoWPF
{
    public partial class MapaWindow : WindowBaseLogica
    {
        private readonly ObservableCollection<Familiar> familiares;

        public MapaWindow(ObservableCollection<Familiar> listaFamiliares)
        {
            InitializeComponent();
            familiares = listaFamiliares;

            // Versión simple mientras dejamos todo compilando:
            Farthest.Text = $"Hay {familiares.Count} familiares cargados.";
            Closest.Text = "Integración del mapa y distancias se hará después.";
            Promedio.Text = "-";
        }
    }
}
