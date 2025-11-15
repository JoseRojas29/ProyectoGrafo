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
          
        }
    }
}