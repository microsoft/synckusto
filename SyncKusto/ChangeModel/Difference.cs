// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace SyncKusto.ChangeModel
{
    public class Difference
    {
        private protected Difference(){}

        public static Difference OnlyInTarget() => new OnlyInTarget();
        public static Difference OnlyInSource() => new OnlyInSource();
        public static Difference Modified() => new Modified();
    }

    public class OnlyInTarget : Difference { }
    public class OnlyInSource : Difference { }
    public class Modified : Difference { }
}