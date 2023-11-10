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

using System.Security.Cryptography.Xml;
using System.Xml;

namespace MultiFactor.SelfService.Linux.Portal.Core.Configuration.XmlConfig
{
    public class XmlDocumentDecryptor
    {
        /// <summary>
        /// Accesses the singleton decryptor instance.
        /// </summary>
        public static readonly XmlDocumentDecryptor Instance = new XmlDocumentDecryptor();

        private readonly Func<XmlDocument, EncryptedXml> _encryptedXmlFactory;

        /// <summary>
        /// Initializes a XmlDocumentDecryptor.
        /// </summary>
        // don't create an instance of this directly
        protected XmlDocumentDecryptor()
          : this(DefaultEncryptedXmlFactory)
        {
        }

        // for testing only
        public XmlDocumentDecryptor(Func<XmlDocument, EncryptedXml> encryptedXmlFactory)
        {
            _encryptedXmlFactory = encryptedXmlFactory;
        }

        private static bool ContainsEncryptedData(XmlDocument document)
        {
            // EncryptedXml will simply decrypt the document in-place without telling
            // us that it did so, so we need to perform a check to see if EncryptedXml
            // will actually do anything. The below check for an encrypted data blob
            // is the same one that EncryptedXml would have performed.
            var namespaceManager = new XmlNamespaceManager(document.NameTable);
            namespaceManager.AddNamespace("enc", "http://www.w3.org/2001/04/xmlenc#");
            return (document.SelectSingleNode("//enc:EncryptedData", namespaceManager) != null);
        }

        /// <summary>
        /// Returns an XmlReader that decrypts data transparently.
        /// </summary>
        public XmlReader CreateDecryptingXmlReader(Stream input, XmlReaderSettings settings)
        {
            // XML-based configurations aren't really all that big, so we can buffer
            // the whole thing in memory while we determine decryption operations.
            var memStream = new MemoryStream();
            input.CopyTo(memStream);
            memStream.Position = 0;

            // First, consume the entire XmlReader as an XmlDocument.
            var document = new XmlDocument();
            using (var reader = XmlReader.Create(memStream, settings))
            {
                document.Load(reader);
            }
            memStream.Position = 0;

            if (ContainsEncryptedData(document))
            {
                return DecryptDocumentAndCreateXmlReader(document);
            }
            else
            {
                // If no decryption would have taken place, return a new fresh reader
                // based on the memory stream (which doesn't need to be disposed).
                return XmlReader.Create(memStream, settings);
            }
        }

        /// <summary>
        /// Creates a reader that can decrypt an encrypted XML document.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <returns>An XmlReader which can read the document.</returns>
        protected virtual XmlReader DecryptDocumentAndCreateXmlReader(XmlDocument document)
        {
            // Perform the actual decryption step, updating the XmlDocument in-place.
            var encryptedXml = _encryptedXmlFactory(document);
            encryptedXml.DecryptDocument();

            // Finally, return the new XmlReader from the updated XmlDocument.
            // Error messages based on this XmlReader won't show line numbers,
            // but that's fine since we transformed the document anyway.
            return document.CreateNavigator().ReadSubtree();
        }

        private static EncryptedXml DefaultEncryptedXmlFactory(XmlDocument document)
          => new EncryptedXml(document);
    }
}
