#region 文件描述

// 开发者：CHENBAIYU
// 解决方案：Lamp
// 工程：Lamp.Utilities
// 文件名：RsaExtension.cs
// 创建日期：2017-09-05

#endregion

using System;
using System.Security.Cryptography;
using System.Xml;

namespace Lamp.Utilities.Crypto.RSA
{
    internal static class RsaExtension
    {
        public static void ExportToXmlString(this System.Security.Cryptography.RSA rsa, bool includePrivateParameters)
        {
            var parameters = rsa.ExportParameters(includePrivateParameters);

            var xmlSettings = new XmlWriterSettings {Indent = true};

            var xmlContent = includePrivateParameters
                ? $"<RSAKeyValue><Modulus>{Convert.ToBase64String(parameters.Modulus)}</Modulus><Exponent>{Convert.ToBase64String(parameters.Exponent)}</Exponent><P>{Convert.ToBase64String(parameters.P)}</P><Q>{Convert.ToBase64String(parameters.Q)}</Q><DP>{Convert.ToBase64String(parameters.DP)}</DP><DQ>{Convert.ToBase64String(parameters.DQ)}</DQ><InverseQ>{Convert.ToBase64String(parameters.InverseQ)}</InverseQ><D>{Convert.ToBase64String(parameters.D)}</D></RSAKeyValue>"
                : $"<RSAKeyValue><Modulus>{Convert.ToBase64String(parameters.Modulus)}</Modulus><Exponent>{Convert.ToBase64String(parameters.Exponent)}</Exponent></RSAKeyValue>";

            var xml = new XmlDocument();
            xml.LoadXml(xmlContent);

            using (var xmlWriter =
                XmlWriter.Create($"{(includePrivateParameters ? "privateKey.xml" : "publicKey.xml")}", xmlSettings))
            {
                xml.WriteTo(xmlWriter);
            }
        }

        public static void ImportFromXmlString(this System.Security.Cryptography.RSA rsa, string filename)
        {
            var parameters = new RSAParameters();
            var xmlDoc = new XmlDocument();
            xmlDoc.Load(filename);
            if (xmlDoc.DocumentElement.Name.Equals("RSAKeyValue"))
                foreach (XmlNode node in xmlDoc.DocumentElement.ChildNodes)
                    switch (node.Name)
                    {
                        case "Modulus":
                            parameters.Modulus = Convert.FromBase64String(node.InnerText);
                            break;
                        case "Exponent":
                            parameters.Exponent = Convert.FromBase64String(node.InnerText);
                            break;
                        case "P":
                            parameters.P = Convert.FromBase64String(node.InnerText);
                            break;
                        case "Q":
                            parameters.Q = Convert.FromBase64String(node.InnerText);
                            break;
                        case "DP":
                            parameters.DP = Convert.FromBase64String(node.InnerText);
                            break;
                        case "DQ":
                            parameters.DQ = Convert.FromBase64String(node.InnerText);
                            break;
                        case "InverseQ":
                            parameters.InverseQ = Convert.FromBase64String(node.InnerText);
                            break;
                        case "D":
                            parameters.D = Convert.FromBase64String(node.InnerText);
                            break;
                    }
            else
                throw new Exception("Invalid XML RSA key.");

            rsa.ImportParameters(parameters);
        }
    }
}