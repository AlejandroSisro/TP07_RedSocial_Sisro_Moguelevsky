using Dapper;
using Microsoft.Data.SqlClient;

namespace TP07_RedSocial_Sisro_Moguelevsky.Models;

public static class BD
{
    public static string ConnectionString { get; } =
        "Server=(localdb)\\MSSQLLocalDB;Database=DBRedSocial;Trusted_Connection=True;TrustServerCertificate=True;";

    public static SqlConnection GetConnection()
    {
        return new SqlConnection(ConnectionString);
    }

    public static async Task EnsureDatabaseAsync()
    {
        const string masterConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=master;Trusted_Connection=True;TrustServerCertificate=True;";

        try
        {
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();
            await EnsureTablesAsync(connection);
        }
        catch
        {
            await using var masterConnection = new SqlConnection(masterConnectionString);
            await masterConnection.OpenAsync();
            await masterConnection.ExecuteAsync("IF DB_ID('DBRedSocial') IS NULL CREATE DATABASE [DBRedSocial]");

            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();
            await EnsureTablesAsync(connection);
        }
    }

    private static async Task EnsureTablesAsync(SqlConnection connection)
    {
        await connection.ExecuteAsync(@"
            IF OBJECT_ID(N'dbo.Usuarios', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[Usuarios](
                    [Id] INT IDENTITY(1,1) NOT NULL,
                    [NombreUsuario] VARCHAR(50) NULL,
                    [Contraseña] VARCHAR(50) NULL,
                    [Nombre] VARCHAR(50) NULL,
                    [Apellido] VARCHAR(50) NULL,
                    CONSTRAINT [PK_Usuarios] PRIMARY KEY CLUSTERED ([Id] ASC)
                );
            END;

            IF OBJECT_ID(N'dbo.Publicaciones', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[Publicaciones](
                    [Id] INT IDENTITY(1,1) NOT NULL,
                    [IdUsuario] INT NOT NULL,
                    [Titulo] VARCHAR(200) NULL,
                    [Descripcion] TEXT NULL,
                    [Imagen] VARCHAR(500) NULL,
                    [FechaPublicacion] DATETIME NULL,
                    CONSTRAINT [PK_Publicaciones] PRIMARY KEY CLUSTERED ([Id] ASC),
                    CONSTRAINT [FK_Publicaciones_Usuarios] FOREIGN KEY([IdUsuario]) REFERENCES [dbo].[Usuarios]([Id])
                );
            END;

            IF OBJECT_ID(N'dbo.PublicacionesMeGusta', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[PublicacionesMeGusta](
                    [Id] INT IDENTITY(1,1) NOT NULL,
                    [IdPublicación] INT NOT NULL,
                    [IdUsuario] INT NOT NULL,
                    CONSTRAINT [PK_PublicacionesMeGusta] PRIMARY KEY CLUSTERED ([Id] ASC),
                    CONSTRAINT [FK_PublicacionesMeGusta_Publicaciones] FOREIGN KEY([IdPublicación]) REFERENCES [dbo].[Publicaciones]([Id]),
                    CONSTRAINT [FK_PublicacionesMeGusta_Usuarios] FOREIGN KEY([IdUsuario]) REFERENCES [dbo].[Usuarios]([Id])
                );
            END;

            IF OBJECT_ID(N'dbo.Comentarios', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[Comentarios](
                    [Id] INT IDENTITY(1,1) NOT NULL,
                    [IdPublicacion] INT NOT NULL,
                    [IdUsuarioComenta] INT NOT NULL,
                    [Texto] TEXT NOT NULL,
                    [FechaComentario] DATETIME NOT NULL,
                    CONSTRAINT [PK_Comentarios] PRIMARY KEY CLUSTERED ([Id] ASC),
                    CONSTRAINT [FK_Comentarios_Publicaciones] FOREIGN KEY([IdPublicacion]) REFERENCES [dbo].[Publicaciones]([Id]),
                    CONSTRAINT [FK_Comentarios_Usuarios] FOREIGN KEY([IdUsuarioComenta]) REFERENCES [dbo].[Usuarios]([Id])
                );
            END;
        ");
    }

    public static async Task<Usuario?> ObtenerUsuarioPorNombreYClaveAsync(string nombreUsuario, string contraseña)
    {
        await EnsureDatabaseAsync();

        await using var connection = GetConnection();
        return await connection.QuerySingleOrDefaultAsync<Usuario>(@"
            SELECT Id, NombreUsuario, Contraseña, Nombre, Apellido
            FROM Usuarios
            WHERE NombreUsuario = @NombreUsuario AND Contraseña = @Contraseña",
            new { NombreUsuario = nombreUsuario, Contraseña = contraseña });
    }

    public static async Task<Usuario?> ObtenerUsuarioPorIdAsync(int id)
    {
        await EnsureDatabaseAsync();

        await using var connection = GetConnection();
        return await connection.QuerySingleOrDefaultAsync<Usuario>(@"
            SELECT Id, NombreUsuario, Contraseña, Nombre, Apellido
            FROM Usuarios
            WHERE Id = @Id",
            new { Id = id });
    }

    public static async Task<bool> ExisteUsuarioAsync(string nombreUsuario)
    {
        await EnsureDatabaseAsync();

        await using var connection = GetConnection();
        var count = await connection.ExecuteScalarAsync<int>(@"
            SELECT COUNT(*)
            FROM Usuarios
            WHERE NombreUsuario = @NombreUsuario",
            new { NombreUsuario = nombreUsuario });

        return count > 0;
    }

    public static async Task<int> RegistrarUsuarioAsync(Usuario usuario)
    {
        await EnsureDatabaseAsync();

        await using var connection = GetConnection();
        return await connection.ExecuteScalarAsync<int>(@"
            INSERT INTO Usuarios (NombreUsuario, Contraseña, Nombre, Apellido)
            VALUES (@NombreUsuario, @Contraseña, @Nombre, @Apellido);
            SELECT CAST(SCOPE_IDENTITY() as int);",
            new { usuario.NombreUsuario, usuario.Contraseña, usuario.Nombre, usuario.Apellido });
    }

    public static async Task<List<Publicacion>> ObtenerPublicacionesAsync(int usuarioId, int offset, int cantidad)
    {
        await EnsureDatabaseAsync();

        await using var connection = GetConnection();
        var publicaciones = (await connection.QueryAsync<Publicacion>(@"
            SELECT
                p.Id,
                p.IdUsuario,
                p.Titulo,
                p.Descripcion,
                p.Imagen,
                p.FechaPublicacion,
                u.NombreUsuario,
                (SELECT COUNT(*) FROM PublicacionesMeGusta WHERE [IdPublicación] = p.Id) AS CantidadLikes,
                CASE WHEN EXISTS (
                    SELECT 1 FROM PublicacionesMeGusta WHERE [IdPublicación] = p.Id AND IdUsuario = @UsuarioId
                ) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS TieneLike
            FROM Publicaciones p
            INNER JOIN Usuarios u ON u.Id = p.IdUsuario
            ORDER BY p.FechaPublicacion DESC
            OFFSET @Offset ROWS FETCH NEXT @Cantidad ROWS ONLY",
            new { UsuarioId = usuarioId, Offset = offset, Cantidad = cantidad })).ToList();

        foreach (var publicacion in publicaciones)
        {
            publicacion.Comentarios = (await connection.QueryAsync<Comentario>(@"
                SELECT c.Id, c.IdPublicacion, c.IdUsuarioComenta, c.Texto, c.FechaComentario, u.NombreUsuario
                FROM Comentarios c
                INNER JOIN Usuarios u ON u.Id = c.IdUsuarioComenta
                WHERE c.IdPublicacion = @IdPublicacion
                ORDER BY c.FechaComentario ASC",
                new { IdPublicacion = publicacion.Id })).ToList();
        }

        return publicaciones;
    }

    public static async Task<int> CrearPublicacionAsync(int usuarioId, string titulo, string descripcion, string imagen)
    {
        await EnsureDatabaseAsync();

        await using var connection = GetConnection();
        return await connection.ExecuteScalarAsync<int>(@"
            INSERT INTO Publicaciones (IdUsuario, Titulo, Descripcion, Imagen, FechaPublicacion)
            VALUES (@IdUsuario, @Titulo, @Descripcion, @Imagen, @FechaPublicacion);
            SELECT CAST(SCOPE_IDENTITY() as int);",
            new
            {
                IdUsuario = usuarioId,
                Titulo = titulo,
                Descripcion = descripcion,
                Imagen = imagen,
                FechaPublicacion = DateTime.Now
            });
    }

    public static async Task<(bool TieneLike, int CantidadLikes)> ToggleLikeAsync(int usuarioId, int publicacionId)
    {
        await EnsureDatabaseAsync();

        await using var connection = GetConnection();
        var existe = await connection.ExecuteScalarAsync<int>(@"
            SELECT COUNT(*)
            FROM PublicacionesMeGusta
            WHERE [IdPublicación] = @PublicacionId AND IdUsuario = @UsuarioId",
            new { PublicacionId = publicacionId, UsuarioId = usuarioId });

        if (existe > 0)
        {
            await connection.ExecuteAsync(@"
                DELETE FROM PublicacionesMeGusta
                WHERE [IdPublicación] = @PublicacionId AND IdUsuario = @UsuarioId",
                new { PublicacionId = publicacionId, UsuarioId = usuarioId });
        }
        else
        {
            await connection.ExecuteAsync(@"
                INSERT INTO PublicacionesMeGusta ([IdPublicación], IdUsuario)
                VALUES (@PublicacionId, @UsuarioId)",
                new { PublicacionId = publicacionId, UsuarioId = usuarioId });
        }

        var cantidadLikes = await connection.ExecuteScalarAsync<int>(@"
            SELECT COUNT(*)
            FROM PublicacionesMeGusta
            WHERE [IdPublicación] = @PublicacionId",
            new { PublicacionId = publicacionId });

        var tieneLike = await connection.ExecuteScalarAsync<int>(@"
            SELECT COUNT(*)
            FROM PublicacionesMeGusta
            WHERE [IdPublicación] = @PublicacionId AND IdUsuario = @UsuarioId",
            new { PublicacionId = publicacionId, UsuarioId = usuarioId });

        return (tieneLike > 0, cantidadLikes);
    }

    public static async Task<(string NombreUsuario, string Texto)> AgregarComentarioAsync(int usuarioId, int publicacionId, string texto)
    {
        await EnsureDatabaseAsync();

        await using var connection = GetConnection();
        var usuario = await ObtenerUsuarioPorIdAsync(usuarioId);

        await connection.ExecuteAsync(@"
            INSERT INTO Comentarios (IdPublicacion, IdUsuarioComenta, Texto, FechaComentario)
            VALUES (@IdPublicacion, @IdUsuarioComenta, @Texto, @FechaComentario)",
            new
            {
                IdPublicacion = publicacionId,
                IdUsuarioComenta = usuarioId,
                Texto = texto,
                FechaComentario = DateTime.Now
            });

        return (usuario?.NombreUsuario ?? "Usuario", texto);
    }
}
