namespace HappyPaws.Application.Notifications;

/// <summary>
/// What actually delivers a message — LINE, SMS, email.
/// <para>
/// Separate from <see cref="INotificationDispatcher"/>, which is what use cases call. The
/// dispatcher decides <b>whether</b> a message may be sent; a channel decides <b>how</b> it
/// travels. Keeping them apart is what allows the consent check to sit in front of every channel
/// at once instead of being repeated inside each adapter — and repeated adapters are exactly where
/// one would eventually be forgotten.
/// </para>
/// <para>
/// A channel must not throw for an undeliverable message. Notification is never part of a clinical
/// transaction: a patient who has been called has been called, whether or not the push arrived.
/// </para>
/// </summary>
public interface INotificationChannel
{
    Task SendAsync(NotificationRequest request, CancellationToken cancellationToken = default);
}
