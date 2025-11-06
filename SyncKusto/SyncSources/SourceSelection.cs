// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace SyncKusto.SyncSources
{
    public sealed class SourceSelection : IEquatable<SourceSelection>
    {
        public static implicit operator string(SourceSelection selection) => selection.SourceMode.ToString();

        public override string ToString() => Enum.GetName(typeof(SourceModeRepresentation), SourceMode) ?? throw new InvalidOperationException();

        private SourceSelection(SourceModeRepresentation source) => SourceMode = source;

        private enum SourceModeRepresentation
        {
            FilePath,
            Kusto
        };

        private SourceModeRepresentation SourceMode { get; }

        public static SourceSelection Kusto() => new SourceSelection(SourceModeRepresentation.Kusto);

        public static SourceSelection FilePath() => new SourceSelection(SourceModeRepresentation.FilePath);

        public bool Equals(SourceSelection? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return SourceMode == other.SourceMode;
        }

        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == this.GetType() && Equals((SourceSelection) obj);
        }

        public override int GetHashCode() => (int) SourceMode;

        public static bool operator ==(SourceSelection left, SourceSelection right) => Equals(left, right);

        public static bool operator !=(SourceSelection left, SourceSelection right) => !Equals(left, right);
    }
}