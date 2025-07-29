using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Spotify.APIConsumer;
using Spotify.Modelos;
using System.Security.Claims;

[Authorize] // Solo usuarios autenticados pueden acceder
public class AlbumsController : Controller
{
    private readonly AppDbContext _context;
    private const string EndPoint = "https://localhost:7028/api/Albums"; 
    public AlbumsController(AppDbContext context)
    {
        _context = context;
        CRUD<Album>.EndPoint = EndPoint;
    }

    // GET: AlbumsController
    public async Task<ActionResult> Index()
    {
        var usuarioId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // Buscar al usuario logueado
        var usuario = await _context.Usuarios.FindAsync(int.Parse(usuarioId));// Verificar si el usuario existe

        // Usamos el CRUD genérico para obtener todos los álbumes del artista
        CRUD<Album>.EndPoint = EndPoint;  
        var albums = CRUD<Album>.GetAll(); // Método del CRUD genérico para obtener todos los álbumes
        var albumsArtista = albums.Where(a => a.ArtistaId == usuario.Id).ToList();  // Filtrar por ArtistaId

        return View(albumsArtista);
    }

    // GET: AlbumsController/Details/5
    public async Task<ActionResult> Details(int id)
    {
        var usuarioId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;// Verificar si el usuario está logueado

        // Buscar al usuario logueado
        var usuario = await _context.Usuarios.FindAsync(int.Parse(usuarioId));// Verificar si el usuario existe


        // Usando el CRUD genérico para obtener un álbum por su ID
        CRUD<Album>.EndPoint = EndPoint;
        var album = CRUD<Album>.GetById(id);

        if (album == null || album.ArtistaId != usuario.Id)
        {
            Console.WriteLine("Álbum no encontrado o no tienes permisos para verlo.");
            return RedirectToAction(nameof(Index));
        }

        return View(album);
    }

    // GET: AlbumsController/Create
    public ActionResult Create()
    {
        var usuarioId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;// Verificar si el usuario está logueado

        var usuario = _context.Usuarios.FirstOrDefault(u => u.Id == int.Parse(usuarioId));// Buscar al usuario logueado

        // Verificar si el usuario logueado es tipo Artista
        if (usuario == null || usuario.TipoUsuario.ToLower() != "artista")
        {
            Console.WriteLine("No tienes permisos para crear un álbum.");
            return RedirectToAction(nameof(Index));
        }

        // Crear un nuevo álbum y asignar el ArtistaId
        var album = new Album
        {
            Nombre = string.Empty,
            Genero = string.Empty,
            ArtistaId = usuario.Id,  // Asignamos el ArtistaId del usuario logueado
            FechaLanzamiento = DateTime.UtcNow
        };

        return View(album);
    }

    // POST: AlbumsController/Create
    // POST: AlbumsController/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Create(Album album)
    {
        try
        {
            Console.WriteLine($"Nombre: {album.Nombre}, Género: {album.Genero}, FechaLanzamiento: {album.FechaLanzamiento}, ArtistaId: {album.ArtistaId}");

            var usuarioId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var usuario = _context.Usuarios.FirstOrDefault(u => u.Id == int.Parse(usuarioId));

            if (usuario == null || usuario.TipoUsuario.ToLower() != "artista")
            {
                Console.WriteLine("No tienes permisos para crear un álbum.");
                return RedirectToAction(nameof(Index));
            }

            // Asignamos el ArtistaId
            album.ArtistaId = usuario.Id;

            // Convertir la fecha a formato ISO 8601 (sin cambiar el tipo de dato)
            album.FechaLanzamiento = DateTime.SpecifyKind(album.FechaLanzamiento, DateTimeKind.Utc);

            // Limpiar propiedades no necesarias (artista, canciones)
            album.Artista = null;  // Establecer el artista como nulo
            album.Canciones = new List<Cancion>();  // Establecer canciones como lista vacía

            // Validaciones
            if (string.IsNullOrWhiteSpace(album.Nombre))// Validar que el nombre no esté vacío
            {
                ModelState.AddModelError("Nombre", "El nombre del álbum es requerido.");
            }

            if (string.IsNullOrWhiteSpace(album.Genero))// Validar que el género no esté vacío
            {
                ModelState.AddModelError("Genero", "El género es requerido.");
            }

            if (album.FechaLanzamiento > DateTime.Now.AddYears(1))// Validar que la fecha de lanzamiento no sea más de un año en el futuro
            {
                ModelState.AddModelError("FechaLanzamiento", "La fecha de lanzamiento no puede ser más de un año en el futuro.");
            }

            // Verificar si el álbum ya existe
            var existeAlbum = CRUD<Album>.GetAll();
            if (existeAlbum.Any(a => a.Nombre.ToLower() == album.Nombre.ToLower() && a.ArtistaId == usuario.Id))// Verificar si ya existe un álbum con el mismo nombre para el artista
            {
                ModelState.AddModelError("Nombre", "Ya tienes un álbum con ese nombre.");
            }

            // Si hay errores de validación, retornamos la vista con los errores
            if (!ModelState.IsValid)
            {
                return View(album);
            }

            // Crear el álbum utilizando el CRUD genérico
            var newAlbum = CRUD<Album>.Create(album);  // Crear álbum enviando el objeto limpio

            Console.WriteLine("¡Álbum creado exitosamente!");
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error al crear el álbum: " + ex.Message);
            Console.WriteLine("Stack Trace: " + ex.StackTrace);  // Agregar la traza del error
            TempData["Error"] = "Hubo un problema al crear el álbum. Verifica los logs para más detalles.";
            return View(album);

    }
}




    // GET: AlbumsController/Edit/5
    public async Task<ActionResult> Edit(int id)
    {
        var usuarioId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // Buscar al usuario logueado
        var usuario = await _context.Usuarios.FindAsync(int.Parse(usuarioId));
        if (usuario == null || usuario.TipoUsuario != "artista")
        {
            Console.WriteLine("No tienes permisos para editar este álbum.");
            return RedirectToAction(nameof(Index));
        }

        // Usando el CRUD genérico para obtener el álbum por ID
        CRUD<Album>.EndPoint = EndPoint;
        var album = CRUD<Album>.GetById(id);

        if (album == null || album.ArtistaId != usuario.Id)
        {
            Console.WriteLine("Álbum no encontrado o no tienes permisos para editarlo.");
            return RedirectToAction(nameof(Index));
        }

        return View(album);
    }

    // POST: AlbumsController/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Edit(int id, Album album)
    {
        try
        {
            var usuarioId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Buscar al usuario logueado
            var usuario = _context.Usuarios.FirstOrDefault(u => u.Id == int.Parse(usuarioId));
            if (usuario == null || usuario.TipoUsuario != "artista")
            {
                Console.WriteLine("No tienes permisos para editar este álbum.");
                return RedirectToAction(nameof(Index));
            }

            if (id != album.Id)
            {
                Console.WriteLine("ID del álbum no coincide.");
                return RedirectToAction(nameof(Index));
            }

            // Verificar que el álbum pertenece al artista logueado
            var existingAlbum = CRUD<Album>.GetById(id);
            if (existingAlbum == null || existingAlbum.ArtistaId != usuario.Id)
            {
                Console.WriteLine("Álbum no encontrado o no tienes permisos para editarlo.");
                return RedirectToAction(nameof(Index));
            }

            // Validaciones personalizadas
            if (string.IsNullOrWhiteSpace(album.Nombre))
            {
                ModelState.AddModelError("Nombre", "El nombre del álbum es requerido.");
            }

            if (string.IsNullOrWhiteSpace(album.Genero))
            {
                ModelState.AddModelError("Genero", "El género es requerido.");
            }

            // Verificar que no exista otro álbum con el mismo nombre (excluyendo el actual)
            var existeAlbum = CRUD<Album>.GetAll();
            if (existeAlbum.Any(a => a.Nombre.ToLower() == album.Nombre.ToLower() && a.ArtistaId == usuario.Id && a.Id != id))
            {
                ModelState.AddModelError("Nombre", "Ya tienes un álbum con ese nombre.");
            }

            if (!ModelState.IsValid)
            {
                return View(album);
            }

            // Actualizar el álbum usando el CRUD genérico
            var updated = CRUD<Album>.Update(id, album);

            Console.WriteLine("¡Álbum actualizado exitosamente!");
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error al actualizar el álbum: " + ex.Message);
            return View(album);
        }
    }

    // GET: AlbumsController/Delete/5
    public async Task<ActionResult> Delete(int id)
    {
        var usuarioId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // Buscar al usuario logueado
        var usuario = await _context.Usuarios.FindAsync(int.Parse(usuarioId));
        if (usuario == null || usuario.TipoUsuario != "artista")
        {
            Console.WriteLine("No tienes permisos para eliminar este álbum.");
            return RedirectToAction(nameof(Index));
        }

        // Usando el CRUD genérico para obtener el álbum por ID
        CRUD<Album>.EndPoint = EndPoint;
        var album = CRUD<Album>.GetById(id);

        if (album == null || album.ArtistaId != usuario.Id)
        {
            Console.WriteLine("Álbum no encontrado o no tienes permisos para eliminarlo.");
            return RedirectToAction(nameof(Index));
        }

        return View(album);
    }

    // POST: AlbumsController/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Delete(int id, IFormCollection collection)
    {
        try
        {
            var usuarioId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Buscar al usuario logueado
            var usuario = await _context.Usuarios.FindAsync(int.Parse(usuarioId));
            if (usuario == null || usuario.TipoUsuario != "artista")
            {
                Console.WriteLine("No tienes permisos para eliminar este álbum.");
                return RedirectToAction(nameof(Index));
            }

            // Eliminar el álbum usando el CRUD genérico
            var deleted = CRUD<Album>.Delete(id);

            Console.WriteLine("¡Álbum eliminado exitosamente!");
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error al eliminar el álbum: " + ex.Message);
            return RedirectToAction(nameof(Index));
        }
    }
}
