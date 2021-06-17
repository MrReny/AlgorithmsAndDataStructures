using System;
using System.Windows;
using Microsoft.Xaml.Behaviors;
using Splat;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Messages;
using ToastNotifications.Position;

namespace Shenon_Fano_Coding.Services
{
    /// <summary>
    /// Сервис отображения уведомления в присоеденнном окне
    /// </summary>
    public class WindowNotificationService:Behavior<Window>, INotificationService
    {
        private Window _attachedWindow;
        private Notifier Notifier { get; set; }


        /// <inheritdoc cref="Behavior.OnAttached"/>
        protected override void OnAttached()
        {
            if(!(AssociatedObject is Window) )
                return;

            Notifier = new Notifier(cfg =>
            {
                cfg.PositionProvider = new WindowPositionProvider(
                    parentWindow: AssociatedObject,
                    corner: Corner.TopRight,
                    offsetX: 10,
                    offsetY: 50);

                cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
                    notificationLifetime: TimeSpan.FromSeconds(3),
                    maximumNotificationCount: MaximumNotificationCount.FromCount(5));

                cfg.Dispatcher = Application.Current.Dispatcher;
            });
            _attachedWindow = (Window)AssociatedObject;

            Locator.CurrentMutable.RegisterConstant(this, typeof(INotificationService));

            base.OnAttached();
        }

        public void ShowMessage(string message)
        {
            Notifier.ShowInformation(message);
        }

        public void ShowSuccess(string message)
        {
            Notifier.ShowSuccess(message);
        }

        public void ShowError(string message)
        {
            Notifier.ShowError(message);
        }

        public void ShowWarning(string message)
        {
            Notifier.ShowWarning(message);
        }
    }
}