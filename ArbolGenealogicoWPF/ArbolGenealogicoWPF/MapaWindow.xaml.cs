using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace ArbolGenealogicoWPF
{
    public partial class MapaWindow : WindowBaseLogica
    {
        private readonly ObservableCollection<Familiar> familiares;

        public MapaWindow(ObservableCollection<Familiar> ListaFamiliares)
        {
            InitializeComponent();
            familiares = ListaFamiliares;
            // Aquí luego podés dibujar puntos según f.Coordenadas
        }
    }
}