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

    public async Task<IActionResult> Index()
    {
        int? usuarioId = HttpContext.Session.GetInt32("UsuarioId");
        HomeViewModel model = new HomeViewModel();

        if (usuarioId.HasValue)
        {
            model.Usuario = await BD.ObtenerUsuarioPorIdAsync(usuarioId.Value);
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
        if (string.IsNullOrWhiteSpace(model.Nombre) ||
            string.IsNullOrWhiteSpace(model.Apellido) ||
            string.IsNullOrWhiteSpace(model.NombreUsuario) ||
            string.IsNullOrWhiteSpace(model.Contraseña))
        {
            TempData["Error"] = "Complete todos los campos.";
            return RedirectToAction(nameof(Index));
        }

        if (await BD.ExisteUsuarioAsync(model.NombreUsuario.Trim()))
        {
            TempData["Error"] = "Ese nombre de usuario ya existe.";
            return RedirectToAction(nameof(Index));
        }

        Usuario usuario = new Usuario
        {
            Nombre = model.Nombre.Trim(),
            Apellido = model.Apellido.Trim(),
            NombreUsuario = model.NombreUsuario.Trim(),
            Contraseña = model.Contraseña.Trim()
        };

        int id = await BD.RegistrarUsuarioAsync(usuario);
        HttpContext.Session.SetInt32("UsuarioId", id);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.NombreUsuario) || string.IsNullOrWhiteSpace(model.Contraseña))
        {
            TempData["Error"] = "Debes completar usuario y contraseña.";
            return RedirectToAction(nameof(Index));
        }

        Usuario usuario = await BD.ObtenerUsuarioPorNombreYClaveAsync(model.NombreUsuario.Trim(), model.Contraseña.Trim());

        if (usuario == null)
        {
            TempData["Error"] = "Usuario o contraseña incorrectos.";
            return RedirectToAction(nameof(Index));
        }

        HttpContext.Session.SetInt32("UsuarioId", usuario.Id);
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
        int? usuarioId = HttpContext.Session.GetInt32("UsuarioId");
        if (!usuarioId.HasValue)
        {
            return RedirectToAction(nameof(Index));
        }

        if (string.IsNullOrWhiteSpace(model.Titulo) ||
            string.IsNullOrWhiteSpace(model.Descripcion) ||
            string.IsNullOrWhiteSpace(model.Imagen))
        {
            TempData["Error"] = "La publicación debe tener imagen, título y descripción.";
            return RedirectToAction(nameof(Index));
        }

        await BD.CrearPublicacionAsync(usuarioId.Value, model.Titulo.Trim(), model.Descripcion.Trim(), model.Imagen.Trim());
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<JsonResult> ObtenerPublicaciones(int offset = 0, int cantidad = 10)
    {
        int usuarioId = HttpContext.Session.GetInt32("UsuarioId") ?? 0;
        List<Publicacion> publicaciones = await BD.ObtenerPublicacionesAsync(usuarioId, offset, cantidad);
        return Json(publicaciones);
    }

    [HttpPost]
    public async Task<IActionResult> ToggleLike([FromBody] LikeRequest request)
    {
        int? usuarioId = HttpContext.Session.GetInt32("UsuarioId");
        if (!usuarioId.HasValue)
        {
            return Json(new { message = "Debes iniciar sesión para dar Me Gusta." });
        }

        (bool TieneLike, int CantidadLikes) resultado = await BD.ToggleLikeAsync(usuarioId.Value, request.PublicacionId);
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
        int? usuarioId = HttpContext.Session.GetInt32("UsuarioId");
        if (!usuarioId.HasValue)
        {
            return Json(new { message = "Debes iniciar sesión para comentar." });
        }

        if (string.IsNullOrWhiteSpace(request.Texto))
        {
            return Json(new { message = "El comentario no puede estar vacío." });
        }

        (string NombreUsuario, string Texto) resultado = await BD.AgregarComentarioAsync(usuarioId.Value, request.PublicacionId, request.Texto.Trim());
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
