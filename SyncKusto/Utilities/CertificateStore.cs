namespace SyncKusto.Utilities
{
    using System;
    using System.Security.Cryptography.X509Certificates;

    internal class CertificateStore
    {
        /// <summary>
        /// Find a certificate by thumbprint in the My store of either CurrentUser or LocalMachine.
        /// </summary>
        /// <param name="thumbprint">The thumbprint of the certificate to retrieve.</param>
        /// <returns>The requested certificate</returns>
        public static X509Certificate2 GetCertificate(string thumbprint)
        {
            var certificate = GetCertificate(StoreLocation.CurrentUser, StoreName.My, thumbprint);
            if (certificate == null)
            {
                certificate = GetCertificate(StoreLocation.LocalMachine, StoreName.My, thumbprint);
            }

            if (certificate == null)
            {
                throw new Exception($"Cannot find certificate with thumbprint: {thumbprint}");
            }

            return certificate;
        }

        /// <summary>
        /// Get all the certificates in both the Current User and Local Machine locations.
        /// </summary>
        /// <returns>A collection of certificates</returns>
        public static X509Certificate2Collection GetAllCertificates()
        {
            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);
            X509Certificate2Collection certificates = store.Certificates;
            store.Close();

            store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
            certificates.AddRange(store.Certificates);
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
        private static X509Certificate2 GetCertificate(StoreLocation storeLocation, StoreName storeName, string thumbprint)
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
    }
}