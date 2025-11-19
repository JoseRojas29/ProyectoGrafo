using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ArbolGenealogicoWPF
{
    public partial class EstadisticasWindow : WindowBaseLogica
    {
        private readonly ObservableCollection<MiembroFamilia> familiares;

        public EstadisticasWindow(ObservableCollection<MiembroFamilia> ListaFamiliares)
        {
            InitializeComponent();
            familiares = ListaFamiliares;
          
        }
    }
}