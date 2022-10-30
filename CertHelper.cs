// https://github.com/dotnet/runtime/issues/23749#issuecomment-747407051

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace RabbitMQProblem;

public static class CertHelper
{
    const string certPath = "./certs/client_shostakovich_certificate.pem";
    const string keyPath = "./certs/client_shostakovich_key.pem";

    // dotnet dev-certs https -ep $pwd/selfsigned.pem --format Pem -np
    public static X509Certificate2 GetCertificate()
    {
        X509Certificate2 sslCert = CreateFromPublicPrivateKey(certPath, keyPath);
        
        // work around for Windows (WinApi) problems with PEMS, still in .NET 5
        return new X509Certificate2(sslCert.Export(X509ContentType.Pkcs12));
    }
    
    public static X509Certificate2 CreateFromPublicPrivateKey(string publicCert="certs/public.pem", string privateCert="certs/private.pem")
    {
        byte[] publicPemBytes = File.ReadAllBytes(publicCert);
        using var publicX509 = new X509Certificate2(publicPemBytes);
        var privateKeyText = File.ReadAllText(privateCert);
        var privateKeyBlocks = privateKeyText.Split("-", StringSplitOptions.RemoveEmptyEntries);
        var privateKeyBytes = Convert.FromBase64String(privateKeyBlocks[1]);

        using RSA rsa = RSA.Create();
        if (privateKeyBlocks[0] == "BEGIN PRIVATE KEY")
        {
            rsa.ImportPkcs8PrivateKey(privateKeyBytes, out _);
        }
        else if (privateKeyBlocks[0] == "BEGIN RSA PRIVATE KEY")
        {
            rsa.ImportRSAPrivateKey(privateKeyBytes, out _);
        }
        X509Certificate2 keyPair = publicX509.CopyWithPrivateKey(rsa);
        return keyPair;
    }
}
