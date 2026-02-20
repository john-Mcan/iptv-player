using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using LibVLCSharp.WPF;

namespace IPTVPlayer.Views;

public partial class PiPWindow : Window
{
    public VideoView VideoViewControl => PiPVideoView;

    public PiPWindow()
    {
        InitializeComponent();

        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - Width - 20;
        Top = workArea.Bottom - Height - 20;
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        if (e.ClickCount == 2)
        {
            Close();
            return;
        }
        DragMove();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private void Overlay_MouseEnter(object sender, MouseEventArgs e)
    {
        var anim = new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(150));
        CloseBtn.BeginAnimation(OpacityProperty, anim);
    }

    private void Overlay_MouseLeave(object sender, MouseEventArgs e)
    {
        var anim = new DoubleAnimation(0.0, TimeSpan.FromMilliseconds(300));
        CloseBtn.BeginAnimation(OpacityProperty, anim);
    }
}
