namespace Shenon_Fano_Coding.Services
{
    /// <summary>
    /// Интерфес сервиса уведомлений
    /// </summary>
    public interface INotificationService
    {

        /// <param name="message">Сообщение увдомления</param>
        void ShowMessage(string message);


        void ShowSuccess(string message);

        void ShowError(string message);

        void ShowWarning(string message);
    }
}