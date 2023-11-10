/*
MIT License

Copyright (c) 2022 MultiFactor

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

using System.Xml;

namespace MultiFactor.SelfService.Linux.Portal.Core.Configuration.XmlConfig
{
    public class XmlConfigurationProvider : FileConfigurationProvider
    {
        private const string NameAttributeKey = "Name";

        /// <summary>
        /// Initializes a new instance with the specified source.
        /// </summary>
        /// <param name="source">The source settings.</param>
        public XmlConfigurationProvider(XmlConfigurationSource source) : base(source)
        {
        }

        public XmlDocumentDecryptor Decryptor { get; set; } = XmlDocumentDecryptor.Instance;

        /// <summary>
        /// Loads the XML data from a stream.
        /// </summary>
        /// <param name="stream">The stream to read.</param>
        public override void Load(Stream stream)
        {
            var configurationValues = new List<IConfigurationValue>();

            var readerSettings = new XmlReaderSettings
            {
                CloseInput = false, // caller will close the stream
                DtdProcessing = DtdProcessing.Prohibit,
                IgnoreComments = true,
                IgnoreWhitespace = true
            };

            using (var reader = Decryptor.CreateDecryptingXmlReader(stream, readerSettings))
            {
                // record all elements we encounter to check for repeated elements
                var allElements = new List<Element>();

                // keep track of the tree we followed to get where we are (breadcrumb style)
                var currentPath = new Stack<Element>();

                var preNodeType = reader.NodeType;
                while (reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            var parent = currentPath.Any() ? currentPath.Peek() : null;
                            var element = new Element(parent, reader.LocalName, reader.GetAttribute(NameAttributeKey));

                            // check if this element has appeared before
                            var siblingKeyToken = allElements
                              .Where(e => e.Parent != null
                                          && e.Parent == parent
                                          && string.Equals(e.ElementName, element.ElementName)
                                          && string.Equals(e.Name, element.Name))
                              .OrderByDescending(e => e.Index)
                              .FirstOrDefault();
                            if (siblingKeyToken != null)
                            {
                                siblingKeyToken.Multiple = element.Multiple = true;
                                element.Index = siblingKeyToken.Index + 1;
                            }

                            currentPath.Push(element);
                            allElements.Add(element);

                            ProcessAttributes(reader, currentPath, configurationValues);

                            // If current element is self-closing
                            if (reader.IsEmptyElement)
                            {
                                currentPath.Pop();
                            }
                            break;

                        case XmlNodeType.EndElement:
                            if (currentPath.Any())
                            {
                                // If this EndElement node comes right after an Element node,
                                // it means there is no text/CDATA node in current element
                                if (preNodeType == XmlNodeType.Element)
                                {
                                    var configurationValue = new ElementContent(currentPath, string.Empty);
                                    configurationValues.Add(configurationValue);
                                }

                                currentPath.Pop();
                            }
                            break;

                        case XmlNodeType.CDATA:
                        case XmlNodeType.Text:
                            {
                                var configurationValue = new ElementContent(currentPath, reader.Value);
                                configurationValues.Add(configurationValue);
                                break;
                            }
                        case XmlNodeType.XmlDeclaration:
                        case XmlNodeType.ProcessingInstruction:
                        case XmlNodeType.Comment:
                        case XmlNodeType.Whitespace:
                            // Ignore certain types of nodes
                            break;

                        default:
                            throw new FormatException(Resources.FormatError_UnsupportedNodeType(reader.NodeType,
                              GetLineInfo(reader)));
                    }
                    preNodeType = reader.NodeType;
                    // If this element is a self-closing element,
                    // we pretend that we just processed an EndElement node
                    // because a self-closing element contains an end within itself
                    if (preNodeType == XmlNodeType.Element &&
                        reader.IsEmptyElement)
                    {
                        preNodeType = XmlNodeType.EndElement;
                    }
                }
            }

            var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var configurationValue in configurationValues)
            {
                var key = configurationValue.Key;
                if (data.ContainsKey(key))
                {
                    throw new FormatException(Resources.FormatError_KeyIsDuplicated(key));
                }
                data[key] = configurationValue.Value;
            }
            Data = data;
        }



        private static string GetLineInfo(XmlReader reader)
        {
            var lineInfo = reader as IXmlLineInfo;
            return lineInfo == null ? string.Empty : Resources.FormatMsg_LineInfo(lineInfo.LineNumber, lineInfo.LinePosition);
        }

        private void ProcessAttributes(XmlReader reader, Stack<Element> elementPath, IList<IConfigurationValue> data, XmlWriter writer = null)
        {
            for (int i = 0; i < reader.AttributeCount; i++)
            {
                reader.MoveToAttribute(i);

                // If there is a namespace attached to current attribute
                if (!string.IsNullOrEmpty(reader.NamespaceURI))
                {
                    throw new FormatException(Resources.FormatError_NamespaceIsNotSupported(GetLineInfo(reader)));
                }

                data.Add(new ElementAttributeValue(elementPath, reader.LocalName, reader.Value));
            }

            // Go back to the element containing the attributes we just processed
            reader.MoveToElement();
        }

        class Element
        {
            // the name of the XML element
            public string ElementName { get; }

            // the content of the 'Name' attribute, if present
            public string Name { get; set; }

            public bool Multiple { get; set; }
            public int Index { get; set; }
            public Element Parent { get; }

            public Element(Element parent, string elementName, string name)
            {
                Parent = parent;
                ElementName = elementName ?? throw new ArgumentNullException(nameof(elementName));
                Name = name;
            }

            public string GetKey()
            {
                var tokens = new List<string>(3);
                // root element does not contribute to prefix
                if (Parent != null) tokens.Add(ElementName);

                // name attribute contributes to prefix
                if (Name != null) tokens.Add(Name);

                // index, if multiple elements exist, contributes to prefix
                if (Multiple) tokens.Add(Index.ToString());

                // the root element without a name attribute does not contribute to prefix at all
                if (!tokens.Any()) return null;
                return string.Join(ConfigurationPath.KeyDelimiter, tokens);
            }
        }

        interface IConfigurationValue
        {
            string Key { get; }
            string Value { get; }
        }

        class ElementContent : IConfigurationValue
        {
            private readonly Element[] _elementPath;

            public ElementContent(Stack<Element> elementPath, string content)
            {
                Value = content ?? throw new ArgumentNullException(nameof(content));
                _elementPath = elementPath?.Reverse()?.ToArray() ?? throw new ArgumentNullException(nameof(elementPath));
            }

            public string Key => ConfigurationPath.Combine(_elementPath.Select(e => e.GetKey()).Where(key => key != null));
            public string Value { get; }
        }

        class ElementAttributeValue : IConfigurationValue
        {
            private readonly Element[] _elementPath;
            private readonly string _attribute;

            public ElementAttributeValue(Stack<Element> elementPath, string attribute, string value)
            {
                _elementPath = elementPath?.Reverse()?.ToArray() ?? throw new ArgumentNullException(nameof(elementPath));
                _attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));
                Value = value ?? throw new ArgumentNullException(nameof(value));
            }

            public string Key => ConfigurationPath.Combine(_elementPath.Select(e => e.GetKey()).Concat(new[] { _attribute }).Where(key => key != null));
            public string Value { get; }
        }
    }
}
