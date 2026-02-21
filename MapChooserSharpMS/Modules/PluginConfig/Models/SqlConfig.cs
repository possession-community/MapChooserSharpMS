using System.Security;
using System.Threading;
using MapChooserSharpMS.Modules.PluginConfig.Enums;
using MapChooserSharpMS.Modules.PluginConfig.Interfaces;

namespace MapChooserSharpMS.Modules.PluginConfig.Models;

internal sealed class SqlConfig : IMcsSqlConfig
{
    public SqlConfig(string host, string port, string databaseName, string user, ref string password, McsSupportedSqlType dataBaseType)
    {
        Host = host;
        UserName = user;
        DataBaseType = dataBaseType;
        Port = port;
        DatabaseName = databaseName;

        Password = ConvertToSecureString(password);

        // Ensure password is removed from memory
        ClearString(ref password);
    }

    public McsSupportedSqlType DataBaseType { get; }
    public string Host { get; }
    public string Port { get; }
    public string DatabaseName { get; }
    public string UserName { get; }
    public SecureString Password { get; }

    private SecureString ConvertToSecureString(string password)
    {
        var securePassword = new SecureString();

        if (string.IsNullOrEmpty(password))
        {
            securePassword.MakeReadOnly();
            return securePassword;
        }

        foreach (char c in password)
        {
            securePassword.AppendChar(c);
        }

        securePassword.MakeReadOnly();
        return securePassword;
    }

    private void ClearString(ref string text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        int length = text.Length;

        char[] charArray = text.ToCharArray();

        text = null!;

        unsafe
        {
            fixed (char* ptr = charArray)
            {
                for (int i = 0; i < length; i++)
                {
                    ptr[i] = '\0';
                }
                // Prevent aggressive compiler optimization from removing the memory clearing operations.
                Thread.MemoryBarrier();
            }
        }
    }
}
