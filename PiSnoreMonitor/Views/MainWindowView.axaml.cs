using Avalonia.Controls;
using PiSnoreMonitor.ViewModels;

namespace PiSnoreMonitor.Views
{
    public partial class MainWindowView : Window
    {
        public MainWindowView()
        {
            InitializeComponent();
        }

        public MainWindowView(MainWindowViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }
    }
}