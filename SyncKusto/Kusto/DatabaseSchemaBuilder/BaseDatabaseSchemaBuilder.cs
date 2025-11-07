// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Kusto.Data.Common;

namespace SyncKusto.Kusto.DatabaseSchemaBuilder
{
    public abstract class BaseDatabaseSchemaBuilder : IDatabaseSchemaBuilder
    {
        public abstract Task<DatabaseSchema> Build();

        private protected static List<string> WaitAllAndGetFailedObjects(List<Task> createOrAlterTasks)
        {
            var failedObjects = new List<string>();
            try
            {
                Task.WaitAll(createOrAlterTasks.ToArray());
            }
            catch (AggregateException ex)
            {
                AggregateException flattendedException = ex.Flatten();
                foreach (Exception exception in flattendedException.InnerExceptions)
                {
                    failedObjects.Add(((SyncKusto.Kusto.Exceptions.CreateOrAlterException)exception).FailedEntityName);
                } 
            }
            return failedObjects;
        }
    }
}