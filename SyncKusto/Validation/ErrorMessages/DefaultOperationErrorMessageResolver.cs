// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using SyncKusto.Functional;
using SyncKusto.Models;
using SyncKusto.Validation.ErrorMessages.Specifications;

namespace SyncKusto.Validation.ErrorMessages
{
    public class DefaultOperationErrorMessageResolver
    {
        private DefaultOperationErrorMessageResolver(Reiterable<IOperationErrorMessageSpecification> errorSpecifications)
        {
            ErrorSpecifications = errorSpecifications;
        }

        private Reiterable<IOperationErrorMessageSpecification> ErrorSpecifications { get; }

        public static Func<DefaultOperationErrorMessageResolver> Using(
            Func<Reiterable<IOperationErrorMessageSpecification>> specifications) =>
            () => new DefaultOperationErrorMessageResolver(specifications());

        public INonEmptyStringState ResolveFor(IOperationError error)
        {
            foreach (IOperationErrorMessageSpecification operationErrorMessageSpecification in ErrorSpecifications)
            {
                switch (error.Exception)
                {
                    case AggregateException ae:
                        foreach (Exception innerException in ae.InnerExceptions)
                        {
                            if (innerException.InnerException == null &&
                                operationErrorMessageSpecification.Match(innerException) is NonEmptyString leaf)
                                return leaf;

                            // do one level of nesting
                            if (operationErrorMessageSpecification.Match(innerException.InnerException) is NonEmptyString nodeParent)
                                return nodeParent;
                        }
                        break;
                    case Exception exception 
                        when operationErrorMessageSpecification.Match(exception) is NonEmptyString matched:
                        return matched;
                }
            }

            return new DefaultOperationErrorSpecification().Match(error.Exception);
        }
    }
}