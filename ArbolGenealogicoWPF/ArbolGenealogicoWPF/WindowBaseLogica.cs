using System.Windows;
using System.Windows.Input;

namespace ArbolGenealogicoWPF
{
    public class WindowBaseLogica : Window
    {
        // Lógica de los botones de control
        protected void Close_Click(object sender, RoutedEventArgs e) => Close();

        protected void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        protected void Maximize_Click(object sender, RoutedEventArgs e) =>
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

        // Permitir mover la ventana al hacer clic y arrastrar en cualquier parte de ella
        protected void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }
    }
}