namespace TP07_RedSocial_Sisro_Moguelevsky.Models;

public class HomeViewModel
{
    public Usuario? Usuario { get; set; }
    public List<Publicacion> Publicaciones { get; set; } = new();
}

public class LoginViewModel
{
    public string NombreUsuario { get; set; } = string.Empty;
    public string Contraseña { get; set; } = string.Empty;
}

public class RegistroViewModel
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string NombreUsuario { get; set; } = string.Empty;
    public string Contraseña { get; set; } = string.Empty;
}

public class CrearPublicacionViewModel
{
    public string Imagen { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
}

public class LikeRequest
{
    public int PublicacionId { get; set; }
}

public class ComentarioRequest
{
    public int PublicacionId { get; set; }
    public string Texto { get; set; } = string.Empty;
}
