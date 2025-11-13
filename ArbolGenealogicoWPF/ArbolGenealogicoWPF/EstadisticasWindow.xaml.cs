using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ArbolGenealogicoWPF
{
    public partial class EstadisticasWindow : Window
    {
        private readonly ObservableCollection<Familiar> _familiares;

        public EstadisticasWindow(ObservableCollection<Familiar> familiares)
        {
            InitializeComponent();
            _familiares = familiares;
            _familiares.CollectionChanged += (_, __) => Calcular();
            Calcular();
            DataContext = this;
        }

        public int Total { get; private set; } = 0;
        public int Vivos { get; private set; } = 0;
        public int Fallecidos { get; private set; } = 0;
        public double EdadPromedio { get; private set; } = 0;

        private void Calcular()
        {
            Total = _familiares.Count;
            Vivos = _familiares.Count(f => !f.IsDeceased);
            Fallecidos = _familiares.Count(f => f.IsDeceased);
            var edades = _familiares.Where(f => f.Edad.HasValue).Select(f => f.Edad.Value).ToList();
            EdadPromedio = edades.Count > 0 ? edades.Average() : 0;
        }

        private void Cerrar_Click(object sender, RoutedEventArgs e) => Close();
    }
}