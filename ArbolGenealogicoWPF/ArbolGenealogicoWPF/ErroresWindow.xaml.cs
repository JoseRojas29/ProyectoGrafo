using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;


namespace ArbolGenealogicoWPF
{
    public partial class ErroresWindow : WindowBaseLogica
    {
        public ErroresWindow(string mensaje)
        {
            InitializeComponent();
            ErrorMessageText.Text = mensaje; // Asignar el mensaje recibido
        }
    }
}
