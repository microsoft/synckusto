// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Kusto.Data;
using SyncKusto.Core.Abstractions;
using SyncKusto.Core.Models;
using SyncKusto.Kusto.Utilities;

namespace SyncKusto.Kusto.Services;

/// <summary>
/// Factory for creating Kusto connection strings
/// </summary>
public class KustoConnectionFactory : IKustoConnectionFactory
{
    /// <summary>
    /// Creates a Kusto connection string based on the provided options
    /// </summary>
    public object CreateConnectionString(KustoConnectionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Cluster);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Database);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Authority);

        var cluster = NormalizeClusterName(options.Cluster);

        return options.AuthMode switch
        {
            AuthenticationMode.AadFederated => CreateUserAuthConnection(cluster, options.Database, options.Authority),
            AuthenticationMode.AadApplication => CreateAppKeyAuthConnection(cluster, options.Database, options.Authority,
                options.AppId!, options.AppKey!),
            AuthenticationMode.AadApplicationSni => CreateAppSniAuthConnection(cluster, options.Database, options.Authority,
                options.AppId!, options.CertificateThumbprint!, options.CertificateLocation),
            _ => throw new ArgumentException($"Unknown authentication mode: {options.AuthMode}", nameof(options))
        };
    }

    /// <summary>
    /// Creates a connection string for user authentication (AAD Federated)
    /// </summary>
    private static KustoConnectionStringBuilder CreateUserAuthConnection(
        string cluster,
        string database,
        string authority)
    {
        return new KustoConnectionStringBuilder(cluster)
        {
            FederatedSecurity = true,
            InitialCatalog = database,
            Authority = authority
        };
    }

    /// <summary>
    /// Creates a connection string for application key authentication
    /// </summary>
    private static KustoConnectionStringBuilder CreateAppKeyAuthConnection(
        string cluster,
        string database,
        string authority,
        string appId,
        string appKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(appId);
        ArgumentException.ThrowIfNullOrWhiteSpace(appKey);

        return new KustoConnectionStringBuilder(cluster)
        {
            FederatedSecurity = true,
            InitialCatalog = database,
            Authority = authority,
            ApplicationKey = appKey,
            ApplicationClientId = appId
        };
    }

    /// <summary>
    /// Creates a connection string for application SNI (Subject Name / Issuer) authentication
    /// </summary>
    private static KustoConnectionStringBuilder CreateAppSniAuthConnection(
        string cluster,
        string database,
        string authority,
        string appId,
        string certificateThumbprint,
        StoreLocation certificateLocation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(appId);
        ArgumentException.ThrowIfNullOrWhiteSpace(certificateThumbprint);

        var certificate = CertificateStore.GetCertificate(certificateThumbprint, certificateLocation);

        return new KustoConnectionStringBuilder(cluster)
        {
            InitialCatalog = database
        }.WithAadApplicationCertificateAuthentication(
            appId,
            certificate,
            authority,
            sendX5c: true);
    }

    /// <summary>
    /// Normalizes cluster name to full HTTPS URL
    /// Allow users to specify cluster.eastus2, cluster.eastus2.kusto.windows.net, or https://cluster.eastus2.kusto.windows.net
    /// </summary>
    /// <param name="cluster">Input cluster name</param>
    /// <returns>Normalized cluster name e.g. https://cluster.eastus2.kusto.windows.net</returns>
    public static string NormalizeClusterName(string cluster)
    {
        if (cluster.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            // If it starts with https, take it verbatim and return from the function
            return cluster;
        }

        // Trim any spaces and trailing '/'
        cluster = cluster.TrimEnd('/').Trim();

        // If it doesn't end with .com or .net then default to .kusto.windows.net
        if (!cluster.EndsWith(".com", StringComparison.OrdinalIgnoreCase) &&
            !cluster.EndsWith(".net", StringComparison.OrdinalIgnoreCase))
        {
            cluster = $"https://{cluster}.kusto.windows.net";
        }
        else if (!cluster.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            // Make sure it starts with https
            cluster = $"https://{cluster}";
        }

        return cluster;
    }
}
