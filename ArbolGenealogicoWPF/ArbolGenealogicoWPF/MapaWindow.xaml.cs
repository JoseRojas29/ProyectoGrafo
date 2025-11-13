using System;
using System.Collections.ObjectModel;
using System.Windows;   // 👈 también aquí
using System.Windows.Controls;

namespace ArbolGenealogicoWPF
{
    public partial class MapaWindow : Window
    {
        private readonly ObservableCollection<Familiar> _familiares;

        public MapaWindow(ObservableCollection<Familiar> familiares)
        {
            InitializeComponent();
            _familiares = familiares;
            // Aquí luego podés dibujar puntos según f.Coordenadas
        }
    }
}