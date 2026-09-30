namespace TP07_RedSocial_Sisro_Moguelevsky.Models;

public class Publicacion
{
    public int Id { get; set; }
    public int IdUsuario { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Imagen { get; set; } = string.Empty;
    public DateTime FechaPublicacion { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public int CantidadLikes { get; set; }
    public bool TieneLike { get; set; }
    public List<Comentario> Comentarios { get; set; } = new();
}
