// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using SyncKusto.Models;

namespace SyncKusto.Validation.ErrorMessages.Specifications
{
    public class DefaultOperationErrorSpecification : IOperationErrorMessageSpecification
    {
        public INonEmptyStringState Match(Exception exception)
        {
            switch (exception)
            {
                case AggregateException ae:
                    foreach (Exception innerException in ae.InnerExceptions)
                    {
                        return Match(innerException);
                    }
                    break;
                default:
                    return new NonEmptyString(exception.Message);
            }

            return new UninitializedString();
        }
    }
}