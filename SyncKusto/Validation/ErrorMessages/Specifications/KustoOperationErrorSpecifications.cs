// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Kusto.Data.Exceptions;
using SyncKusto.Validation.Infrastructure;

namespace SyncKusto.Validation.ErrorMessages.Specifications
{
    public static class KustoOperationErrorSpecifications
    {
        public static IOperationErrorMessageSpecification ClusterNotFound() =>
            new OperationErrorMessageSpecification(Spec<Exception>
                    .IsTrue(ex => ex is KustoClientNameResolutionFailureException _),
                "The Kusto cluster could not be located.");

        public static IOperationErrorMessageSpecification DatabaseNotFound() =>
            new OperationErrorMessageSpecification(Spec<Exception>
                    .IsTrue(ex => ex is KustoBadRequestException request
                                  && request.Message.Contains("'Database' was not found")),
                "The Kusto database could not be found.");

        public static IOperationErrorMessageSpecification CannotAuthenticate() =>
            new OperationErrorMessageSpecification(Spec<Exception>
                    .IsTrue(ex => ex is KustoClientAuthenticationException),
                "Could not authenticate with AAD. Check the AAD authority in the Settings.");
    }
}