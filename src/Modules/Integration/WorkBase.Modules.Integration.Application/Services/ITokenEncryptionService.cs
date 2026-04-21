namespace WorkBase.Modules.Integration.Application.Services;

public interface ITokenEncryptionService
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}
