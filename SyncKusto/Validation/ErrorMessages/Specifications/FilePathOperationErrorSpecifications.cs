// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using SyncKusto.Validation.Infrastructure;

namespace SyncKusto.Validation.ErrorMessages.Specifications
{
    public static class FilePathOperationErrorSpecifications
    {
        public static IOperationErrorMessageSpecification FolderNotFound() =>
            new OperationErrorMessageSpecification(Spec<Exception>
                    .IsTrue(ex => ex is DirectoryNotFoundException _),
                "The folder path provided could not be found.");
    }
}