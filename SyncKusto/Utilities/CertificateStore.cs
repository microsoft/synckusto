namespace SyncKusto.Utilities
{
    using System;
    using System.Security.Cryptography.X509Certificates;
    using X509StoreLocation = System.Security.Cryptography.X509Certificates.StoreLocation;

    internal class CertificateStore
    {
        /// <summary>
        /// Find a certificate by thumbprint in the My store of either CurrentUser or LocalMachine.
        /// </summary>
        /// <param name="thumbprint">The thumbprint of the certificate to retrieve.</param>
        /// <returns>The requested certificate</returns>
        public static X509Certificate2 GetCertificate(string thumbprint)
        {
            var certificate = GetCertificate(X509StoreLocation.CurrentUser, StoreName.My, thumbprint);
            if (certificate == null)
            {
                certificate = GetCertificate(X509StoreLocation.LocalMachine, StoreName.My, thumbprint);
            }

            if (certificate == null)
            {
                throw new Exception($"Cannot find certificate with thumbprint: {thumbprint}");
            }

            return certificate;
        }

        /// <summary>
        /// Get all the certificates in the specified location.
        /// </summary>
        /// <returns>A collection of certificates</returns>
        public static X509Certificate2Collection GetAllCertificates(Core.Models.StoreLocation storeLocation)
        {
            var location = ConvertStoreLocation(storeLocation);
            return GetAllCertificatesInternal(location);
        }

        /// <summary>
        /// Get all the certificates in the specified location.
        /// </summary>
        /// <returns>A collection of certificates</returns>
        public static X509Certificate2Collection GetAllCertificates(X509StoreLocation storeLocation)
        {
            return GetAllCertificatesInternal(storeLocation);
        }

        private static X509Certificate2Collection GetAllCertificatesInternal(X509StoreLocation storeLocation)
        {
            var store = new X509Store(StoreName.My, storeLocation);
            store.Open(OpenFlags.ReadOnly);
            var certificates = store.Certificates;
            store.Close();

            return certificates;
        }

        /// <summary>
        /// Find a certificate by thumbprint in the specified store and location.
        /// </summary>
        /// <param name="storeLocation">The location of the store to search for the certificate.</param>
        /// <param name="storeName">The name of the store to search for the certificate.</param>
        /// <param name="thumbprint">The thumbprint of the certificate to retrieve.</param>
        /// <returns>The requested certificate or null if it is not found.</returns>
        private static X509Certificate2? GetCertificate(X509StoreLocation storeLocation, StoreName storeName, string thumbprint)
        {
            var certStore = new X509Store(storeName, storeLocation);
            certStore.Open(OpenFlags.ReadOnly);
            var certCollection = certStore.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false);
            certStore.Close();

            if (certCollection.Count == 0)
            {
                return null;
            }

            return certCollection[0];
        }

        /// <summary>
        /// Converts our StoreLocation enum to the X509 StoreLocation enum.
        /// </summary>
        private static X509StoreLocation ConvertStoreLocation(Core.Models.StoreLocation location)
        {
            return location switch
            {
                Core.Models.StoreLocation.CurrentUser => X509StoreLocation.CurrentUser,
                Core.Models.StoreLocation.LocalMachine => X509StoreLocation.LocalMachine,
                _ => throw new ArgumentException($"Unknown store location: {location}", nameof(location))
            };
        }
    }
}