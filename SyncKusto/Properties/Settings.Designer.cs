﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SyncKusto.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "17.12.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string PreviousFilePath {
            get {
                return ((string)(this["PreviousFilePath"]));
            }
            set {
                this["PreviousFilePath"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string KustoClusterForTempDatabases {
            get {
                return ((string)(this["KustoClusterForTempDatabases"]));
            }
            set {
                this["KustoClusterForTempDatabases"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string AADAuthority {
            get {
                return ((string)(this["AADAuthority"]));
            }
            set {
                this["AADAuthority"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string TemporaryKustoDatabase {
            get {
                return ((string)(this["TemporaryKustoDatabase"]));
            }
            set {
                this["TemporaryKustoDatabase"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool KustoObjectDropWarning {
            get {
                return ((bool)(this["KustoObjectDropWarning"]));
            }
            set {
                this["KustoObjectDropWarning"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool TableFieldsOnNewLine {
            get {
                return ((bool)(this["TableFieldsOnNewLine"]));
            }
            set {
                this["TableFieldsOnNewLine"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool CreateMergeEnabled {
            get {
                return ((bool)(this["CreateMergeEnabled"]));
            }
            set {
                this["CreateMergeEnabled"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool UseLegacyCslExtension {
            get {
                return ((bool)(this["UseLegacyCslExtension"]));
            }
            set {
                this["UseLegacyCslExtension"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("CurrentUser")]
        public string CertificateLocation {
            get {
                return ((string)(this["CertificateLocation"]));
            }
            set {
                this["CertificateLocation"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public global::System.Collections.Specialized.StringCollection RecentClusters {
            get {
                return ((global::System.Collections.Specialized.StringCollection)(this["RecentClusters"]));
            }
            set {
                this["RecentClusters"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public global::System.Collections.Specialized.StringCollection RecentDatabases {
            get {
                return ((global::System.Collections.Specialized.StringCollection)(this["RecentDatabases"]));
            }
            set {
                this["RecentDatabases"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public global::System.Collections.Specialized.StringCollection RecentAppIds {
            get {
                return ((global::System.Collections.Specialized.StringCollection)(this["RecentAppIds"]));
            }
            set {
                this["RecentAppIds"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0")]
        public int LineEndingMode {
            get {
                return ((int)(this["LineEndingMode"]));
            }
            set {
                this["LineEndingMode"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool IgnoreLineEndings {
            get {
                return ((bool)(this["IgnoreLineEndings"]));
            }
            set {
                this["IgnoreLineEndings"] = value;
            }
        }
    }
}
