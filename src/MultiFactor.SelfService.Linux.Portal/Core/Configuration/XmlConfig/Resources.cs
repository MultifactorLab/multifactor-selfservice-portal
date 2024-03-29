﻿using System.Globalization;
using System.Reflection;
using System.Resources;

namespace MultiFactor.SelfService.Linux.Portal.Core.Configuration.XmlConfig
{
    public static class Resources
    {
        private static readonly ResourceManager _resourceManager
            = new ResourceManager("MultiFactor.SelfService.Linux.Portal.Core.Configuration.XmlConfig.Resources", typeof(Resources).GetTypeInfo().Assembly);

        /// <summary>
        /// File path must be a non-empty string.
        /// </summary>
        public static string Error_InvalidFilePath
        {
            get => GetString("Error_InvalidFilePath");
        }

        /// <summary>
        /// File path must be a non-empty string.
        /// </summary>
        public static string FormatError_InvalidFilePath()
            => GetString("Error_InvalidFilePath");

        /// <summary>
        /// A duplicate key '{0}' was found.{1}
        /// </summary>
        public static string Error_KeyIsDuplicated
        {
            get => GetString("Error_KeyIsDuplicated");
        }

        /// <summary>
        /// A duplicate key '{0}' was found.{1}
        /// </summary>
        public static string FormatError_KeyIsDuplicated(object p0)
            => string.Format(CultureInfo.CurrentCulture, GetString("Error_KeyIsDuplicated"), p0);

        /// <summary>
        /// XML namespaces are not supported.{0}
        /// </summary>
        public static string Error_NamespaceIsNotSupported
        {
            get => GetString("Error_NamespaceIsNotSupported");
        }

        /// <summary>
        /// XML namespaces are not supported.{0}
        /// </summary>
        public static string FormatError_NamespaceIsNotSupported(object p0)
            => string.Format(CultureInfo.CurrentCulture, GetString("Error_NamespaceIsNotSupported"), p0);

        /// <summary>
        /// Unsupported node type '{0}' was found.{1}
        /// </summary>
        public static string Error_UnsupportedNodeType
        {
            get => GetString("Error_UnsupportedNodeType");
        }

        /// <summary>
        /// Unsupported node type '{0}' was found.{1}
        /// </summary>
        public static string FormatError_UnsupportedNodeType(object p0, object p1)
            => string.Format(CultureInfo.CurrentCulture, GetString("Error_UnsupportedNodeType"), p0, p1);

        /// <summary>
        ///  Line {0}, position {1}.
        /// </summary>
        public static string Msg_LineInfo
        {
            get => GetString("Msg_LineInfo");
        }

        /// <summary>
        ///  Line {0}, position {1}.
        /// </summary>
        public static string FormatMsg_LineInfo(object p0, object p1)
            => string.Format(CultureInfo.CurrentCulture, GetString("Msg_LineInfo"), p0, p1);

        private static string GetString(string name, params string[] formatterNames)
        {
            var value = _resourceManager.GetString(name);

            System.Diagnostics.Debug.Assert(value != null);

            if (formatterNames != null)
            {
                for (var i = 0; i < formatterNames.Length; i++)
                {
                    value = value.Replace("{" + formatterNames[i] + "}", "{" + i + "}");
                }
            }

            return value;
        }
    }
}
