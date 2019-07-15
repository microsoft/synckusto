// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using SyncKusto.Models;
using SyncKusto.Validation.Infrastructure;

namespace SyncKusto.Validation.ErrorMessages
{
    public class OperationErrorMessageSpecification : IOperationErrorMessageSpecification
    {
        public OperationErrorMessageSpecification(Specification<Exception> specification, string displayMessage)
        {
            Specification = specification;
            DisplayMessage = displayMessage;
        }

        private string DisplayMessage { get; }

        private Specification<Exception> Specification { get; }

        public INonEmptyStringState Match(Exception exception) =>
            Specification.IsSatisfiedBy(exception)
                ? (INonEmptyStringState) new NonEmptyString(DisplayMessage)
                : new UninitializedString();
    }
}