using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ArbolGenealogicoWPF
{
    public partial class EstadisticasWindow : WindowBaseLogica
    {
        private readonly ObservableCollection<Familiar> familiares;

        public EstadisticasWindow(ObservableCollection<Familiar> ListaFamiliares)
        {
            InitializeComponent();
            familiares = ListaFamiliares;
            familiares.CollectionChanged += (_, __) => Calcular();
            Calcular();
            DataContext = this;
        }

        public int Total { get; private set; } = 0;
        public int Vivos { get; private set; } = 0;
        public int Fallecidos { get; private set; } = 0;
        public double EdadPromedio { get; private set; } = 0;

        private void Calcular()
        {
            Total = familiares.Count;
            Vivos = familiares.Count(f => !f.IsDeceased);
            Fallecidos = familiares.Count(f => f.IsDeceased);
            var edades = familiares.Where(f => f.Edad.HasValue).Select(f => f.Edad.Value).ToList();
            EdadPromedio = edades.Count > 0 ? edades.Average() : 0;
        }
    }
}