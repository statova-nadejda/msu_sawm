using System.Security.Cryptography;
using System.Text;
using Lab5.Models;
using Npgsql;

namespace Lab5.Services;

public class AccessStore
{
    public const string CookieName = "__Host-sawm-session";

    private readonly NpgsqlDataSource _dataSource;

    public AccessStore(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    private static string HashToken(string token)
    {
        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    private static bool IsValidToken(string token)
    {
        return token.Length == 64 && token.All(Uri.IsHexDigit);
    }

    public async Task<LoginAccount?> FindLoginAsync(string login)
    {
        await using var command = _dataSource.CreateCommand("""
            SELECT id, login, role, password, password_hash
            FROM public."user"
            WHERE login = @login
            """);

        command.Parameters.AddWithValue("login", login);

        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        var user = new AccessUser(
            reader.GetInt32(0),
            reader.GetString(1),
            reader.IsDBNull(2) ? null : reader.GetString(2));

        return new LoginAccount(
            user,
            reader.IsDBNull(3) ? null : reader.GetString(3),
            reader.IsDBNull(4) ? null : reader.GetString(4));
    }

    public async Task StartSessionAsync(
        HttpContext context,
        int userId)
    {
        await EndSessionAsync(context);

        var token = Convert.ToHexString(
            RandomNumberGenerator.GetBytes(32));

        await using var command = _dataSource.CreateCommand("""
            INSERT INTO public.user_session (
                token_hash,
                user_id,
                expires_at
            )
            VALUES (
                @tokenHash,
                @userId,
                CURRENT_TIMESTAMP + INTERVAL '20 minutes'
            )
            """);

        command.Parameters.AddWithValue("tokenHash", HashToken(token));
        command.Parameters.AddWithValue("userId", userId);

        await command.ExecuteNonQueryAsync();

        context.Response.Cookies.Append(
            CookieName,
            token,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/"
            });
    }

    public async Task<AccessUser?> GetCurrentUserAsync(
        HttpContext context)
    {
        if (!context.Request.Cookies.TryGetValue(
                CookieName, out var token) ||
            !IsValidToken(token))
        {
            return null;
        }

        await using var command = _dataSource.CreateCommand("""
            SELECT u.id, u.login, u.role
            FROM public.user_session AS s
            JOIN public."user" AS u ON u.id = s.user_id
            WHERE s.token_hash = @tokenHash
              AND s.expires_at > CURRENT_TIMESTAMP
            """);

        command.Parameters.AddWithValue(
            "tokenHash", HashToken(token));

        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new AccessUser(
            reader.GetInt32(0),
            reader.GetString(1),
            reader.IsDBNull(2) ? null : reader.GetString(2));
    }

    public async Task<bool> HasPermissionAsync(
        string? role,
        string permission)
    {
        if (role == null)
        {
            return false;
        }

        await using var command = _dataSource.CreateCommand("""
            SELECT EXISTS (
                SELECT 1
                FROM public.role_permission
                WHERE role = @role
                  AND permission = @permission
            )
            """);

        command.Parameters.AddWithValue("role", role);
        command.Parameters.AddWithValue("permission", permission);

        return (bool)(await command.ExecuteScalarAsync())!;
    }

    public async Task EndSessionAsync(HttpContext context)
    {
        if (context.Request.Cookies.TryGetValue(
                CookieName, out var token) &&
            IsValidToken(token))
        {
            await using var command = _dataSource.CreateCommand("""
                DELETE FROM public.user_session
                WHERE token_hash = @tokenHash
                """);

            command.Parameters.AddWithValue(
                "tokenHash", HashToken(token));

            await command.ExecuteNonQueryAsync();
        }

        context.Response.Cookies.Delete(
            CookieName,
            new CookieOptions
            {
                Secure = true,
                HttpOnly = true,
                SameSite = SameSiteMode.Strict,
                Path = "/"
            });
    }

    public async Task<List<AccessUser>> GetAccountsAsync()
    {
        await using var command = _dataSource.CreateCommand("""
            SELECT id, login, role
            FROM public."user"
            ORDER BY id
            """);

        await using var reader = await command.ExecuteReaderAsync();

        var users = new List<AccessUser>();

        while (await reader.ReadAsync())
        {
            users.Add(new AccessUser(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2)));
        }

        return users;
    }

    public async Task<AccessUser?> GetAccountAsync(int id)
    {
        await using var command = _dataSource.CreateCommand("""
            SELECT id, login, role
            FROM public."user"
            WHERE id = @id
            """);

        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new AccessUser(
            reader.GetInt32(0),
            reader.GetString(1),
            reader.IsDBNull(2) ? null : reader.GetString(2));
    }

    public async Task<bool> UpdateLoginAsync(
        int id,
        string login,
        bool isAdmin)
    {
        await using var command = _dataSource.CreateCommand("""
            UPDATE public."user"
            SET login = @login
            WHERE id = @id
              AND (@isAdmin OR role IS NULL)
            """);

        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("login", login);
        command.Parameters.AddWithValue("isAdmin", isAdmin);

        return await command.ExecuteNonQueryAsync() == 1;
    }
}