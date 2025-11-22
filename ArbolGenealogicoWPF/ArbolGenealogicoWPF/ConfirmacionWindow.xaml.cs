using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;


namespace ArbolGenealogicoWPF
{
    public partial class ConfirmacionWindow : WindowBaseLogica
    {
        public bool Confirmado { get; private set; } = false;

        public ConfirmacionWindow()
        {
            InitializeComponent();
        }

        private void Aceptar_Click(object sender, RoutedEventArgs e)
        {
            Confirmado = true;
            Close();
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            Confirmado = false;
            Close();
        }
    }
}