using Microsoft.IdentityModel.Tokens;
using OpenAISelfhost.Service.Interface;
using System.Security.Cryptography;

namespace OpenAISelfhost.Service
{
    /// <summary>
    /// Manages RSA key pairs for JWT signing and validation.
    /// Generates a new RSA-2048 key pair on startup and rotates every 48 hours.
    /// The previous key is retained for validation of tokens issued before the last rotation.
    /// Keys older than one rotation interval (48 h) are released; since tokens are valid for
    /// only 3 hours, no legitimate token can still be alive at that point.
    /// Note: tokens issued before a service restart will be invalidated, as key material is
    /// held only in memory and is not persisted across restarts.
    /// </summary>
    public class RsaKeyService : IRsaKeyService, IHostedService, IDisposable
    {
        // Current signing key (contains both private and public key material).
        private RSA _currentRsa;
        private RsaSecurityKey _currentKey;

        // Public-only key retained from the previous rotation for validation of
        // recently issued tokens. Replaced (and the old RSA object released to GC)
        // at every subsequent rotation.
        private RSA? _previousRsa;
        private RsaSecurityKey? _previousKey;

        private readonly object _lock = new();
        private Timer? _rotationTimer;
        private static readonly TimeSpan RotationInterval = TimeSpan.FromHours(48);

        public RsaKeyService()
        {
            _currentRsa = RSA.Create(2048);
            _currentKey = new RsaSecurityKey(_currentRsa);
        }

        /// <summary>Returns the cached signing key backed by the current RSA private key.</summary>
        public RsaSecurityKey GetCurrentSigningKey()
        {
            lock (_lock)
            {
                return _currentKey;
            }
        }

        /// <summary>
        /// Returns cached security keys for all currently valid RSA key pairs
        /// (current key and, if present, the previous key for recently-issued tokens).
        /// </summary>
        public IEnumerable<SecurityKey> GetValidationKeys()
        {
            lock (_lock)
            {
                if (_previousKey != null)
                    return new SecurityKey[] { _currentKey, _previousKey };
                return new SecurityKey[] { _currentKey };
            }
        }

        private void RotateKey(object? state)
        {
            lock (_lock)
            {
                // Promote the current key to "previous" (public-only) for validation
                // of tokens issued in the last rotation window. The old _previousRsa is
                // released to the GC; by now its tokens are well past their 3-hour expiry.
                var prevRsa = RSA.Create();
                prevRsa.ImportParameters(_currentRsa.ExportParameters(includePrivateParameters: false));
                _previousRsa = prevRsa;
                _previousKey = new RsaSecurityKey(prevRsa);

                // Generate a fresh signing key pair.
                _currentRsa = RSA.Create(2048);
                _currentKey = new RsaSecurityKey(_currentRsa);
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _rotationTimer = new Timer(RotateKey, null, RotationInterval, RotationInterval);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _rotationTimer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _rotationTimer?.Dispose();
            _currentRsa.Dispose();
            _previousRsa?.Dispose();
        }
    }
}
