using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TP07_RedSocial_Sisro_Moguelevsky.Models;

namespace TP07_RedSocial_Sisro_Moguelevsky.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    private bool EstaVacio(string texto)
    {
        if (texto == null)
        {
            return true;
        }

        if (texto == "")
        {
            return true;
        }

        return false;
    }

    private int ObtenerUsuarioIdDeSesion()
    {
        string usuarioIdTexto = HttpContext.Session.GetString("UsuarioId");

        if (usuarioIdTexto == null || usuarioIdTexto == "")
        {
            return 0;
        }

        int usuarioId = 0;
        int.TryParse(usuarioIdTexto, out usuarioId);
        return usuarioId;
    }

    private void GuardarUsuarioIdEnSesion(int usuarioId)
    {
        HttpContext.Session.SetString("UsuarioId", usuarioId.ToString());
    }

    public async Task<IActionResult> Index()
    {
        int usuarioId = ObtenerUsuarioIdDeSesion();
        HomeViewModel model = new HomeViewModel();

        if (usuarioId > 0)
        {
            model.Usuario = await BD.ObtenerUsuarioPorIdAsync(usuarioId);

            if (model.Usuario != null)
            {
                model.Publicaciones = await BD.ObtenerPublicacionesAsync(model.Usuario.Id, 0, 10);
            }
        }

        ViewBag.Usuario = model.Usuario;
        ViewBag.Publicaciones = model.Publicaciones;

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registrar(RegistroViewModel model)
    {
        if (EstaVacio(model.Nombre) || EstaVacio(model.Apellido) || EstaVacio(model.NombreUsuario) || EstaVacio(model.Contraseña))
        {
            TempData["Error"] = "Complete todos los campos.";
            return RedirectToAction(nameof(Index));
        }

        bool existe = await BD.ExisteUsuarioAsync(model.NombreUsuario);
        if (existe)
        {
            TempData["Error"] = "Ese nombre de usuario ya existe.";
            return RedirectToAction(nameof(Index));
        }

        Usuario usuario = new Usuario();
        usuario.Nombre = model.Nombre;
        usuario.Apellido = model.Apellido;
        usuario.NombreUsuario = model.NombreUsuario;
        usuario.Contraseña = model.Contraseña;

        int id = await BD.RegistrarUsuarioAsync(usuario);
        GuardarUsuarioIdEnSesion(id);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (EstaVacio(model.NombreUsuario) || EstaVacio(model.Contraseña))
        {
            TempData["Error"] = "Debes completar usuario y contraseña.";
            return RedirectToAction(nameof(Index));
        }

        Usuario usuario = await BD.ObtenerUsuarioPorNombreYClaveAsync(model.NombreUsuario, model.Contraseña);

        if (usuario == null)
        {
            TempData["Error"] = "Usuario o contraseña incorrectos.";
            return RedirectToAction(nameof(Index));
        }

        GuardarUsuarioIdEnSesion(usuario.Id);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Remove("UsuarioId");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearPublicacion(CrearPublicacionViewModel model)
    {
        int usuarioId = ObtenerUsuarioIdDeSesion();

        if (usuarioId == 0)
        {
            return RedirectToAction(nameof(Index));
        }

        if (EstaVacio(model.Titulo) || EstaVacio(model.Descripcion) || EstaVacio(model.Imagen))
        {
            TempData["Error"] = "La publicación debe tener imagen, título y descripción.";
            return RedirectToAction(nameof(Index));
        }

        await BD.CrearPublicacionAsync(usuarioId, model.Titulo, model.Descripcion, model.Imagen);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<JsonResult> ObtenerPublicaciones(int offset = 0, int cantidad = 10)
    {
        int usuarioId = ObtenerUsuarioIdDeSesion();
        List<Publicacion> publicaciones = await BD.ObtenerPublicacionesAsync(usuarioId, offset, cantidad);
        return Json(publicaciones);
    }

    [HttpPost]
    public async Task<IActionResult> ToggleLike([FromBody] LikeRequest request)
    {
        int usuarioId = ObtenerUsuarioIdDeSesion();

        if (usuarioId == 0)
        {
            return Json(new { message = "Debes iniciar sesión para dar Me Gusta." });
        }

        (bool TieneLike, int CantidadLikes) resultado = await BD.ToggleLikeAsync(usuarioId, request.PublicacionId);

        return Json(new
        {
            tieneLike = resultado.TieneLike,
            cantidadLikes = resultado.CantidadLikes,
            message = "OK"
        });
    }

    [HttpPost]
    public async Task<IActionResult> AgregarComentario([FromBody] ComentarioRequest request)
    {
        int usuarioId = ObtenerUsuarioIdDeSesion();

        if (usuarioId == 0)
        {
            return Json(new { message = "Debes iniciar sesión para comentar." });
        }

        if (EstaVacio(request.Texto))
        {
            return Json(new { message = "El comentario no puede estar vacío." });
        }

        (string NombreUsuario, string Texto) resultado = await BD.AgregarComentarioAsync(usuarioId, request.PublicacionId, request.Texto);

        return Json(new
        {
            nombreUsuario = resultado.NombreUsuario,
            texto = resultado.Texto
        });
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
